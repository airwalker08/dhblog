#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Build Dhblog.Api (Debug) and configure the IIS site (default: DhblogApi on port 82).

.DESCRIPTION
  Points IIS at the Debug build output under src/Dhblog.Api/bin/Debug/net8.0 so you can
  attach a debugger and step through API code. Stops the running host before building
  to avoid file locks. Grants OneDrive folder access for IIS.
#>
param(
    [string]$SiteName = "DhblogApi",
    [int]$Port = 82,
    [string]$Configuration = "Debug",
    [string]$IisPhysicalPath = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$apiProject = Join-Path $repoRoot "src\Dhblog.Api\Dhblog.Api.csproj"
if (-not $IisPhysicalPath) {
    $IisPhysicalPath = Join-Path $repoRoot "src\Dhblog.Api\bin\$Configuration\net8.0"
}

$appcmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"
if (-not (Test-Path $appcmd)) {
    throw "IIS appcmd.exe not found. Install IIS first."
}

function Test-IisAppPool {
    param([string]$Name)
    $output = & $appcmd list apppool "$Name" 2>&1
    return $LASTEXITCODE -eq 0 -and ($output -match "APPPOOL")
}

function Test-IisSite {
    param([string]$Name)
    $output = & $appcmd list site "$Name" 2>&1
    return $LASTEXITCODE -eq 0 -and ($output -match "SITE")
}

function Stop-DhblogApiHost {
    Write-Host "Stopping IIS site and API processes to release file locks..."

    if (Test-IisSite -Name $SiteName) {
        & $appcmd stop site "$SiteName" 2>&1 | Out-Null
    }
    if (Test-IisAppPool -Name $SiteName) {
        & $appcmd stop apppool "$SiteName" 2>&1 | Out-Null
    }

    Start-Sleep -Seconds 2

    Get-Process -Name "Dhblog.Api" -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "  Stopping process $($_.Name) ($($_.Id))..."
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }

    Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -eq "dotnet.exe" -and $_.CommandLine -match "Dhblog\.Api" } |
        ForEach-Object {
            Write-Host "  Stopping dotnet host ($($_.ProcessId))..."
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        }

    Start-Sleep -Seconds 1
}

function Grant-IisFolderAccess {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return }
    icacls $Path /grant "IIS_IUSRS:(OI)(CI)(RX)" | Out-Null
    icacls $Path /grant "IUSR:(OI)(CI)(RX)" | Out-Null
    icacls $Path /grant "IIS AppPool\${SiteName}:(OI)(CI)(RX)" | Out-Null
}

function Ensure-IisSite {
    if (-not (Test-IisAppPool -Name $SiteName)) {
        Write-Host "Creating IIS app pool '$SiteName'..."
        & $appcmd add apppool /name:"$SiteName" /managedRuntimeVersion:""
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create app pool '$SiteName'."
        }
    } else {
        & $appcmd set apppool "$SiteName" /managedRuntimeVersion:""
    }

    if (-not (Test-IisSite -Name $SiteName)) {
        Write-Host "Creating IIS site '$SiteName' on port $Port..."
        New-Item -ItemType Directory -Force -Path $IisPhysicalPath | Out-Null
        & $appcmd add site /name:"$SiteName" /bindings:"http/*:${Port}:" /physicalPath:"$IisPhysicalPath"
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create site '$SiteName'."
        }
        & $appcmd set app "$SiteName/" /applicationPool:"$SiteName"
    } else {
        & $appcmd set vdir "$SiteName/" /physicalPath:"$IisPhysicalPath"
    }
}

Stop-DhblogApiHost

Write-Host "Building API (Debug) for IIS at $IisPhysicalPath ..."
dotnet build $apiProject -c $Configuration --no-restore 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    dotnet build $apiProject -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed."
    }
}

$oneDriveChain = @(
    (Join-Path $env:USERPROFILE "OneDrive"),
    (Join-Path $env:USERPROFILE "OneDrive\Documents"),
    (Join-Path $env:USERPROFILE "OneDrive\Documents\GitHub"),
    $repoRoot,
    (Join-Path $repoRoot "src"),
    (Join-Path $repoRoot "src\Dhblog.Api"),
    (Join-Path $repoRoot "src\Dhblog.Api\bin"),
    $IisPhysicalPath
)
foreach ($folder in $oneDriveChain) {
    Grant-IisFolderAccess -Path $folder
}

$logsDir = Join-Path $IisPhysicalPath "logs"
New-Item -ItemType Directory -Force -Path $logsDir | Out-Null
Grant-IisFolderAccess -Path $logsDir
icacls $logsDir /grant "IIS AppPool\${SiteName}:(OI)(CI)(M)" | Out-Null

Write-Host "Configuring IIS site '$SiteName' ..."
Ensure-IisSite

& $appcmd start apppool "$SiteName" 2>&1 | Out-Null
& $appcmd start site "$SiteName" 2>&1 | Out-Null

Write-Host ""
Write-Host "Done. API should be available at http://localhost:${Port}/"
Write-Host "Swagger: http://localhost:${Port}/swagger"
Write-Host "IIS physical path (Debug, debuggable): $IisPhysicalPath"
Write-Host "Attach debugger to the dotnet.exe process hosting Dhblog.Api.dll"
Write-Host "If the site fails, check $logsDir\stdout*.log"
