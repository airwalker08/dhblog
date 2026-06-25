<#
.SYNOPSIS
  Single-command startup for the dhblog web app and all supporting services.

.DESCRIPTION
  - local mode (default): DynamoDB Local, local database seed, IIS API, Vite dev server
  - remote mode: AWS dev services via IIS API, Vite dev server

  IIS hosts the Debug build at src/Dhblog.Api/bin/Debug/net8.0 so you can attach
  a debugger and step through API code. Runs setup-iis.ps1 when IIS is not yet
  configured. Elevation is requested only for that step when needed.

.PARAMETER Mode
  "local" or "remote". Defaults to "local".

.EXAMPLE
  .\start.ps1
  .\start.ps1 -Mode local
  .\start.ps1 -Mode remote
#>
param(
    [ValidateSet('local', 'remote')]
    [string]$Mode = 'local'
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$IisSiteName = "DhblogApi"
$ApiProject = Join-Path $Root "src\Dhblog.Api\Dhblog.Api.csproj"
$IisOutputDir = Join-Path $Root "src\Dhblog.Api\bin\Debug\net8.0"
$IisWebConfig = Join-Path $IisOutputDir "web.config"
$SetupIisScript = Join-Path $Root "scripts\setup-iis.ps1"
$DeployDatabaseScript = Join-Path $Root "scripts\deploy-database.ps1"
$ApiUrl = "http://localhost:82"
$ApiHealthUrl = "$ApiUrl/api/health"
$PollTimeoutSeconds = 30
$PollIntervalSeconds = 2

function Test-CommandAvailable {
    param([string]$Name)
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Test-DockerDaemon {
    docker info 2>$null | Out-Null
    return $LASTEXITCODE -eq 0
}

function Test-Administrator {
    $current = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
    return $current.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-IisSiteConfigured {
    $appcmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"
    if (-not (Test-Path $appcmd)) {
        return $false
    }
    $output = & $appcmd list site "$IisSiteName" 2>&1
    return $LASTEXITCODE -eq 0 -and ($output -match "SITE")
}

function Test-IisPointsAtDebugOutput {
    $appcmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"
    if (-not (Test-Path $appcmd)) {
        return $false
    }
    if (-not (Test-IisSiteConfigured)) {
        return $false
    }

    $physicalPath = (& $appcmd list vdir "$IisSiteName/" /text:physicalPath 2>&1 | Select-Object -First 1)
    if ([string]::IsNullOrWhiteSpace($physicalPath)) {
        return $false
    }

    $expected = [System.IO.Path]::GetFullPath($IisOutputDir)
    $actual = [System.IO.Path]::GetFullPath($physicalPath.Trim())
    return $actual -eq $expected
}

function Test-IisApiBuildReady {
    return (Test-Path $IisWebConfig) -and (Test-Path (Join-Path $IisOutputDir "Dhblog.Api.dll"))
}

function Build-ApiForIis {
    Write-Host "Building API (Debug) for IIS..."
    Stop-DhblogApiHost
    dotnet build $ApiProject -c Debug
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed."
    }
    Write-Host "  OK API built"
}

function Stop-DhblogApiHost {
    $appcmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"
    if (-not (Test-Path $appcmd)) {
        return
    }

    & $appcmd stop site "$IisSiteName" 2>&1 | Out-Null
    & $appcmd stop apppool "$IisSiteName" 2>&1 | Out-Null
    Start-Sleep -Seconds 1

    Get-Process -Name "Dhblog.Api" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -eq "dotnet.exe" -and $_.CommandLine -match "Dhblog\.Api" } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}

function Invoke-SetupIis {
    Write-Host "  Building and configuring IIS API (administrator required)..."
    if (-not (Test-Path $SetupIisScript)) {
        throw "setup-iis.ps1 not found at $SetupIisScript"
    }

    Stop-DhblogApiHost

    if (Test-Administrator) {
        & $SetupIisScript
    } else {
        $proc = Start-Process powershell.exe -Verb RunAs -Wait -PassThru -ArgumentList @(
            "-NoProfile",
            "-ExecutionPolicy", "Bypass",
            "-File", $SetupIisScript
        )
        if ($proc.ExitCode -ne 0) {
            if ($proc.ExitCode -eq -1073741510) {
                throw "setup-iis.ps1 was cancelled. Approve the administrator prompt and run .\start.ps1 again."
            }
            throw "setup-iis.ps1 failed or was cancelled (exit code $($proc.ExitCode))."
        }
    }

    if (-not (Test-IisApiBuildReady)) {
        throw "IIS setup completed but Debug build output is missing at $IisOutputDir"
    }
    Write-Host "  OK IIS API built"
}

function Ensure-IisApi {
    $needsSetup = -not (Test-IisApiBuildReady) -or -not (Test-IisSiteConfigured) -or -not (Test-IisPointsAtDebugOutput)
    if ($needsSetup) {
        Write-Host "IIS API setup required..."
        Invoke-SetupIis
    } else {
        Write-Host "  OK IIS API build"
    }
}

function Ensure-PnpmDependencies {
    $webModules = Join-Path $Root "apps\web\node_modules"
    if (Test-Path $webModules) {
        Write-Host "  OK node dependencies"
        return
    }

    Write-Host "Installing node dependencies..."
    pnpm install
    Write-Host "  OK node dependencies"
}

function Ensure-LocalDatabase {
    Write-Host "Ensuring local database tables and seed data..."
    & $DeployDatabaseScript -Env local
    Write-Host "  OK local database"
}

function Wait-ForTcpPort {
    param(
        [string]$HostName = "localhost",
        [int]$Port,
        [int]$TimeoutSeconds = 30
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $result = Test-NetConnection -ComputerName $HostName -Port $Port -WarningAction SilentlyContinue
        if ($result.TcpTestSucceeded) {
            return $true
        }
        Start-Sleep -Seconds $PollIntervalSeconds
    }
    return $false
}

function Wait-ForApiHealth {
    param(
        [string]$Url,
        [int]$TimeoutSeconds = 30
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec 5
            if ($response.status -eq "healthy") {
                return $true
            }
        } catch {
            # API not ready yet
        }
        Start-Sleep -Seconds $PollIntervalSeconds
    }
    return $false
}

function Get-IisEnvironmentVariablesNode {
    param([xml]$Config)

    $envVars = $Config.SelectSingleNode("//aspNetCore/environmentVariables")
    if ($envVars) {
        return $envVars
    }

    $aspNetCore = $Config.SelectSingleNode("//aspNetCore")
    if (-not $aspNetCore) {
        throw "web.config is missing aspNetCore element at $IisWebConfig"
    }

    $envVars = $Config.CreateElement("environmentVariables")
    [void]$aspNetCore.AppendChild($envVars)
    return $envVars
}

function Set-IisEnvironmentVariable {
    param(
        [System.Xml.XmlElement]$EnvironmentVariablesNode,
        [string]$Name,
        [string]$Value
    )
    if (-not $EnvironmentVariablesNode) {
        throw "Cannot update IIS environment variable '$Name': environmentVariables node is missing."
    }

    $existing = $EnvironmentVariablesNode.SelectSingleNode("environmentVariable[@name='$Name']")
    if ($existing) {
        $existing.SetAttribute("value", $Value)
    } else {
        $newNode = $EnvironmentVariablesNode.OwnerDocument.CreateElement("environmentVariable")
        $newNode.SetAttribute("name", $Name)
        $newNode.SetAttribute("value", $Value)
        [void]$EnvironmentVariablesNode.AppendChild($newNode)
    }
}

function Remove-IisEnvironmentVariable {
    param(
        [System.Xml.XmlElement]$EnvironmentVariablesNode,
        [string]$Name
    )
    if (-not $EnvironmentVariablesNode) {
        return
    }

    $existing = $EnvironmentVariablesNode.SelectSingleNode("environmentVariable[@name='$Name']")
    if ($existing) {
        [void]$EnvironmentVariablesNode.RemoveChild($existing)
    }
}

function Set-IisApiEnvironment {
    param([string]$Mode)

    [xml]$config = Get-Content -Path $IisWebConfig
    $envVars = Get-IisEnvironmentVariablesNode -Config $config

    Set-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "ASPNETCORE_ENVIRONMENT" -Value "Development"

    if ($Mode -eq "local") {
        Set-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "DHBLOG_ENV" -Value "local"
        Set-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "DHBLOG_USE_AWS" -Value "false"
        Set-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "DYNAMODB_ENDPOINT" -Value "http://localhost:8000"
        Set-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "AWS_ACCESS_KEY_ID" -Value "local"
        Set-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "AWS_SECRET_ACCESS_KEY" -Value "local"
        Set-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "AWS_REGION" -Value "us-east-1"
        Remove-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "AWS_PROFILE"
    } else {
        Set-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "DHBLOG_ENV" -Value "dev"
        Set-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "DHBLOG_USE_AWS" -Value "true"
        Set-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "AWS_REGION" -Value "us-west-2"
        Set-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "AWS_PROFILE" -Value "AdminPublish"
        Remove-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "DYNAMODB_ENDPOINT"
        Remove-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "AWS_ACCESS_KEY_ID"
        Remove-IisEnvironmentVariable -EnvironmentVariablesNode $envVars -Name "AWS_SECRET_ACCESS_KEY"
    }

    $config.Save($IisWebConfig)
    Write-Host "  Updated IIS web.config for $Mode mode."

    $appcmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"
    if (Test-Path $appcmd) {
        & $appcmd recycle apppool $IisSiteName 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  Recycled IIS app pool '$IisSiteName'."
        } else {
            Write-Warning "  Could not recycle app pool '$IisSiteName'. Recycle it manually in IIS Manager."
        }
    } else {
        Write-Warning "  IIS appcmd not found. Recycle app pool '$IisSiteName' manually in IIS Manager."
    }
}

function Ensure-ApiHealthy {
    param([string]$Mode)

    Write-Host "Verifying API at $ApiHealthUrl ..."
    if (Wait-ForApiHealth -Url $ApiHealthUrl -TimeoutSeconds $PollTimeoutSeconds) {
        Write-Host "  OK API healthy"
        return
    }

    Write-Host "  API not healthy - republishing via setup-iis.ps1..."
    Invoke-SetupIis
    Set-IisApiEnvironment -Mode $Mode

    if (Wait-ForApiHealth -Url $ApiHealthUrl -TimeoutSeconds 60) {
        Write-Host "  OK API healthy"
        return
    }

    if ($Mode -eq "local") {
        throw "API is not reachable at $ApiHealthUrl. Check that DynamoDB Local is running and the IIS app pool '$IisSiteName' was recycled. See src\Dhblog.Api\bin\Debug\net8.0\logs\stdout*.log for details."
    } else {
        throw "API is not reachable at $ApiHealthUrl. Check AWS credentials/profile and that dev tables exist (.\scripts\deploy-database.ps1 -Env dev). See src\Dhblog.Api\bin\Debug\net8.0\logs\stdout*.log for details."
    }
}

Write-Host "dhblog start ($Mode)"
Push-Location $Root

try {
    Write-Host "Checking prerequisites..."
    if (-not (Test-CommandAvailable "dotnet")) {
        throw ".NET SDK not found. Install .NET 8 SDK."
    }
    if (-not (Test-CommandAvailable "pnpm")) {
        if (Test-CommandAvailable "npm") {
            Write-Host "  Installing pnpm..."
            npm install -g pnpm
        } else {
            throw "pnpm not found. Install Node 20 LTS and pnpm."
        }
    }
    Write-Host "  OK dotnet, pnpm"

    Ensure-PnpmDependencies
    Ensure-IisApi

    if ($Mode -eq "local") {
        Write-Host "Starting local services..."
        if (-not (Test-CommandAvailable "docker")) {
            throw "Docker not found. Install Docker Desktop for local mode."
        }
        if (-not (Test-DockerDaemon)) {
            throw "Docker Desktop is not running. Start Docker Desktop and run .\start.ps1 again."
        }
        Write-Host "  OK docker"

        Write-Host "  Starting DynamoDB Local..."
        $prevErrorAction = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        try {
            docker compose up -d dynamodb 2>&1 | ForEach-Object {
                if ($_ -is [System.Management.Automation.ErrorRecord]) {
                    Write-Host $_.ToString()
                } else {
                    Write-Host $_
                }
            }
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to start DynamoDB Local via Docker Compose (exit code $LASTEXITCODE)."
            }
        } finally {
            $ErrorActionPreference = $prevErrorAction
        }

        Write-Host "  Waiting for DynamoDB Local on port 8000..."
        if (-not (Wait-ForTcpPort -Port 8000 -TimeoutSeconds $PollTimeoutSeconds)) {
            throw "DynamoDB Local did not become reachable on port 8000 within ${PollTimeoutSeconds}s."
        }
        Write-Host "  OK DynamoDB Local"

        Ensure-LocalDatabase
    } else {
        Write-Host "Verifying AWS credentials..."
        if (-not (Test-CommandAvailable "aws")) {
            throw "AWS CLI not found. Install AWS CLI v2 for remote mode."
        }
        aws sts get-caller-identity 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "AWS credentials are missing or invalid. Configure machine-wide credentials (see .env.example)."
        }
        Write-Host "  OK AWS credentials"
    }

    Build-ApiForIis

    Write-Host "Configuring IIS API for $Mode mode..."
    Set-IisApiEnvironment -Mode $Mode

    Ensure-ApiHealthy -Mode $Mode

    Write-Host ""
    Write-Host "Starting web dev server..."
    Write-Host "  Web:    http://localhost:5173"
    Write-Host "  API:    $ApiUrl (Swagger: $ApiUrl/swagger)"
    Write-Host "  IIS path (must match in IIS Manager):"
    Write-Host "          $([System.IO.Path]::GetFullPath($IisOutputDir))"
    Write-Host "  Debug:  attach to dotnet.exe running Dhblog.Api.dll (not w3wp.exe)"
    Write-Host "  Login:  Coulson / SecretPwd(42)"
    Write-Host ""

    $env:VITE_API_URL = $ApiUrl
    pnpm --filter web dev
} finally {
    Pop-Location
}
