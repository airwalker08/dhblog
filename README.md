# dhblog

Personal sandbox for experimenting with tiered AWS architecture: React web app, ASP.NET Core REST API, DynamoDB data tier, and ECS Fargate deployment.

## Architecture

| Tier | Project | Responsibility |
|------|---------|----------------|
| Database | `src/Dhblog.Database` | Entity definitions, table deploy CLI, seed data |
| Data access | `src/Dhblog.DataAccess` | DynamoDB CRUD repositories only |
| API | `src/Dhblog.Api` | Business logic, auth, REST endpoints |
| Web | `apps/web` | React SPA (Materialize CSS + MUI free) |

## Prerequisites

| Tool | Version |
|------|---------|
| Docker Desktop | latest |
| .NET SDK | 8.0 |
| Node.js | 20 LTS |
| pnpm | 9+ |
| AWS CLI v2 | optional (for AWS deploy) |
| AWS CDK CLI | `npm i -g aws-cdk` |

## Quick start (local)

```powershell
# First-time bootstrap
.\scripts\local-setup.ps1

# Terminal 1 — API
dotnet run --project src/Dhblog.Api

# Terminal 2 — Web
pnpm --filter web dev
```

Open http://localhost:5173 and sign in:

- **Username:** `Coulson`
- **Password:** `SecretPwd(42)`

Copy `.env.example` to `.env.local` and adjust as needed.

## Local workflows

### Fast dev (recommended)

```powershell
docker compose up -d dynamodb
dotnet run --project src/Dhblog.Api
pnpm --filter web dev
```

Vite proxies `/api` to the API (default `http://localhost:8080`).

### Full Docker parity

```powershell
docker compose --profile full up --build
```

- Web: http://localhost:3000
- API: http://localhost:8080
- DynamoDB Local: http://localhost:8000

## Database deploy

Create/update tables and seed roles, features, and admin user:

```powershell
.\scripts\deploy-database.ps1 -Env local   # DynamoDB Local
.\scripts\deploy-database.ps1 -Env dev    # AWS dev tables via CDK + seed
```

## AWS deployment

Infrastructure is defined in `infra/` (AWS CDK TypeScript):

- **NetworkStack** — VPC, subnets, NAT
- **DataStack** — DynamoDB tables
- **MediaStack** — S3 + CloudFront for blog images
- **EcrStack** — Container registries
- **ConfigStack** — SSM Parameter Store settings
- **AppStack** — ECS Fargate + ALB (`/api/*` → API, `/*` → web)
- **PipelineStack** — CodePipeline + CodeBuild (optional, requires GitHub connection ARN)

```powershell
# Bootstrap CDK (once per account/region)
cd infra
pnpm install
npx cdk bootstrap

# Deploy everything
.\scripts\deploy.ps1 -Env dev
```

### CI/CD

`buildspec.yml` drives CodeBuild: build .NET + React, push Docker images to ECR, run `cdk deploy`, force ECS rolling update.

Enable the pipeline by passing context when deploying CDK:

```bash
npx cdk deploy -c env=dev -c githubConnectionArn=arn:aws:codestar-connections:... -c githubOwner=your-org -c githubRepo=dhblog
```

## Initial seed data

**Roles:** Administrator, Standard User, Read-only user

**Features:** Settings, Diagnostics, Blog, Feed, Profile

**Admin user:** Coulson (Administrator role)

## Project structure

```
dhblog/
├── apps/web/              # React + Vite
├── src/
│   ├── Dhblog.Database/   # Entities + deploy/seed CLI
│   ├── Dhblog.DataAccess/ # Repositories
│   └── Dhblog.Api/        # REST API
├── infra/                 # AWS CDK
├── docker/                # Dockerfiles
├── scripts/               # deploy-database.ps1, deploy.ps1, local-setup.ps1
├── docker-compose.yml
└── Dhblog.sln
```

## Security notes

- Passwords are hashed with ASP.NET Core `PasswordHasher` at seed time; never stored in plaintext.
- JWT secret: set via `JWT_SECRET` locally; use SSM SecureString `/dhblog/{env}/secrets/jwt` in AWS.
- Change the default JWT secret and admin password before any production use.
