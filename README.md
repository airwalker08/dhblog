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
.\start.ps1                  # local mode (default) — handles IIS, DynamoDB, DB seed, and web
.\start.ps1 -Mode remote     # AWS dev services
```

First run may prompt for administrator approval to configure IIS. The `DhblogApi` site points at the **Debug build output** (`src/Dhblog.Api/bin/Debug/net8.0`) so you can attach a debugger and step through API code.

### Debugging the API under IIS

**IIS physical path (required):**

```
src/Dhblog.Api/bin/Debug/net8.0
```

Full path example: `C:\Users\<you>\OneDrive\Documents\GitHub\dhblog\src\Dhblog.Api\bin\Debug\net8.0`

That folder must contain `Dhblog.Api.dll`, `Dhblog.Api.pdb`, and `web.config`. IIS site **DhblogApi** must point at this folder — not `publish\iis-api`, not the project root, not `bin\Debug` without `net8.0`.

**Verify in IIS Manager:** Sites → DhblogApi → Basic Settings → Physical path.

**Attach the debugger correctly:** ASP.NET Core runs **out-of-process** (`hostingModel="outofprocess"` in web.config). Attach to the **`dotnet.exe`** process whose command line includes `Dhblog.Api.dll`. Do **not** attach only to `w3wp.exe` — breakpoints in your code will not hit.

**Visual Studio:** Open `Dhblog.Api`, select the **IIS** launch profile, set breakpoints, press F5. The project sets `<IISAppName>DhblogApi</IISAppName>` so VS binds to the correct site.

**VS Code / Cursor:** Run `.\start.ps1`, trigger a request (so the app starts), then use **Attach to Dhblog.Api (IIS)** and pick the `dotnet.exe` process running `Dhblog.Api.dll`.

**If breakpoints stay hollow/unbound:** Rebuild (`dotnet build src/Dhblog.Api -c Debug`), recycle the app pool, re-attach. In Visual Studio, open Debug → Windows → Modules and confirm `Dhblog.Api.dll` shows **Symbols loaded**.

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
