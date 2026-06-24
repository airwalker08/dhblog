param(
    [string]$Env = "dev"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot

Write-Host "Deploying DynamoDB tables for env: $Env"

Push-Location $Root
try {
    if ($Env -ne "local") {
        Push-Location infra
        npx cdk deploy "DhblogData-$Env" -c env=$Env --require-approval never
        Pop-Location
    }

    $env:DHBLOG_ENV = $Env
    if ($Env -eq "local") {
        $env:DYNAMODB_ENDPOINT = "http://localhost:8000"
        $env:AWS_ACCESS_KEY_ID = "local"
        $env:AWS_SECRET_ACCESS_KEY = "local"
    }

    dotnet run --project src/Dhblog.Database -- deploy-tables --env $Env
    dotnet run --project src/Dhblog.Database -- seed --env $Env

    Write-Host "Database deploy complete."
}
finally {
    Pop-Location
}
