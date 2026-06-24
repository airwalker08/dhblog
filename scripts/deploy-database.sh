#!/usr/bin/env bash
set -euo pipefail
ENV="${1:-dev}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "Deploying DynamoDB tables for env: $ENV"
cd "$ROOT"

if [ "$ENV" != "local" ]; then
  cd infra
  npx cdk deploy "DhblogData-$ENV" -c "env=$ENV" --require-approval never
  cd ..
fi

export DHBLOG_ENV="$ENV"
if [ "$ENV" = "local" ]; then
  export DYNAMODB_ENDPOINT="http://localhost:8000"
  export AWS_ACCESS_KEY_ID="local"
  export AWS_SECRET_ACCESS_KEY="local"
fi

dotnet run --project src/Dhblog.Database -- deploy-tables --env "$ENV"
dotnet run --project src/Dhblog.Database -- seed --env "$ENV"
echo "Database deploy complete."
