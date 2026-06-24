#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Publish Dhblog.Api and configure the IIS site (default: DhblogApi on port 82).

.DESCRIPTION
  OneDrive folders block IIS by default. This script grants traverse/read access,
  publishes the API, points the site at the publish folder, and sets the app pool
  to "No Managed Code" for ASP.NET Core.
#>
param(
    [string]$SiteName = "DhblogApi",
    [int]$Port = 82,
    [string]$Configuration = "Debug",
    [string]$PublishDir = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not $PublishDir) {
    $PublishDir = Join-Path $repoRoot "publish\iis-api"
}

$appcmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"
if (-not (Test-Path $appcmd)) {
    throw "IIS appcmd.exe not found. Install IIS first."
}

function Grant-IisFolderAccess {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return }
    icacls $Path /grant "IIS_IUSRS:(OI)(CI)(RX)" | Out-Null
    icacls $Path /grant "IUSR:(OI)(CI)(RX)" | Out-Null
    icacls $Path /grant "IIS AppPool\${SiteName}:(OI)(CI)(RX)" | Out-Null
}

Write-Host "Publishing API to $PublishDir ..."
dotnet publish (Join-Path $repoRoot "src\Dhblog.Api\Dhblog.Api.csproj") `
    -c $Configuration `
    -o $PublishDir

$oneDriveChain = @(
    (Join-Path $env:USERPROFILE "OneDrive"),
    (Join-Path $env:USERPROFILE "OneDrive\Documents"),
    (Join-Path $env:USERPROFILE "OneDrive\Documents\GitHub"),
    $repoRoot,
    (Join-Path $repoRoot "src"),
    (Join-Path $repoRoot "publish"),
    $PublishDir
)
foreach ($folder in $oneDriveChain) {
    Grant-IisFolderAccess -Path $folder
}

$logsDir = Join-Path $PublishDir "logs"
New-Item -ItemType Directory -Force -Path $logsDir | Out-Null
Grant-IisFolderAccess -Path $logsDir
icacls $logsDir /grant "IIS AppPool\${SiteName}:(OI)(CI)(M)" | Out-Null

Write-Host "Configuring IIS site '$SiteName' ..."
& $appcmd set apppool "$SiteName" /managedRuntimeVersion:""
& $appcmd set vdir "$SiteName/" /physicalPath:"$PublishDir"
& $appcmd recycle apppool "$SiteName"

Write-Host ""
Write-Host "Done. API should be available at http://localhost:${Port}/"
Write-Host "Swagger (Debug publish): http://localhost:${Port}/swagger"
Write-Host "Physical path: $PublishDir"
Write-Host "If the site fails, check $logsDir\stdout*.log"
