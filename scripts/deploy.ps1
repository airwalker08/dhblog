param(
    [string]$Env = "dev",
    [string]$Region = "us-east-1"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot

Write-Host "Full deploy for env: $Env"
Push-Location $Root

try {
    if (Get-Command pnpm -ErrorAction SilentlyContinue) {
        pnpm install
        pnpm --filter web build
    } else {
        Write-Warning "pnpm not found; skipping web build"
    }

    dotnet publish src/Dhblog.Api/Dhblog.Api.csproj -c Release -o ./publish/api

    $AccountId = (aws sts get-caller-identity --query Account --output text)
    $ApiRepo = "$AccountId.dkr.ecr.$Region.amazonaws.com/dhblog-api-$Env"
    $WebRepo = "$AccountId.dkr.ecr.$Region.amazonaws.com/dhblog-web-$Env"

    aws ecr get-login-password --region $Region | docker login --username AWS --password-stdin "$AccountId.dkr.ecr.$Region.amazonaws.com"

    docker build -f docker/api/Dockerfile -t "${ApiRepo}:latest" .
    docker build -f docker/web/Dockerfile -t "${WebRepo}:latest" .
    docker push "${ApiRepo}:latest"
    docker push "${WebRepo}:latest"

    Push-Location infra
    pnpm install
    npx cdk deploy --all -c env=$Env --require-approval never
    Pop-Location

    & "$PSScriptRoot/deploy-database.ps1" -Env $Env

    Write-Host "Deploy complete. Check ALB DNS in CDK outputs."
}
finally {
    Pop-Location
}
