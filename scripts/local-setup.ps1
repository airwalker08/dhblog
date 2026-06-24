param(
    [switch]$SkipDocker
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot

Write-Host "dhblog local setup"
Push-Location $Root

Write-Host "Checking prerequisites..."
$required = @(
    @{ Name = "dotnet"; Cmd = "dotnet --version" },
    @{ Name = "docker"; Cmd = "docker --version" }
)

foreach ($tool in $required) {
    try {
        Invoke-Expression $tool.Cmd | Out-Null
        Write-Host "  OK $($tool.Name)"
    } catch {
        Write-Warning "  Missing $($tool.Name)"
    }
}

if (Get-Command pnpm -ErrorAction SilentlyContinue) {
    pnpm install
} elseif (Get-Command npm -ErrorAction SilentlyContinue) {
    npm install -g pnpm
    pnpm install
} else {
    Write-Warning "Node/pnpm not found. Install Node 20 LTS to build the web app."
}

if (-not $SkipDocker) {
    Write-Host "Starting DynamoDB Local..."
    docker compose up -d dynamodb
    Start-Sleep -Seconds 3
}

$env:DHBLOG_ENV = "local"
$env:DYNAMODB_ENDPOINT = "http://localhost:8000"
$env:AWS_ACCESS_KEY_ID = "local"
$env:AWS_SECRET_ACCESS_KEY = "local"

& "$PSScriptRoot/deploy-database.ps1" -Env local

Write-Host @"

Setup complete.

Fast dev workflow:
  1. docker compose up -d dynamodb
  2. dotnet run --project src/Dhblog.Api
  3. pnpm --filter web dev

Login: Coulson / SecretPwd(42)

Full Docker parity:
  docker compose --profile full up --build

"@
Pop-Location
