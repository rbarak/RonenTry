# CI/CD Pipeline & Architecture

Complete reference for the Investment Tracker deployment pipeline, AWS architecture, and service decisions.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Developer Machine                         │
│  git push → GitHub → CI (build/test/push) → CD (deploy)        │
└────────────────────────────┬────────────────────────────────────┘
                             │
                    GitHub Actions
                    ci.yml + cd.yml
                             │
              ┌──────────────┼──────────────┐
              │              │              │
         ┌────▼────┐   ┌─────▼─────┐  ┌────▼────────┐
         │   ECR   │   │  ECS      │  │   S3        │
         │ (Docker │   │  Fargate  │  │  Frontend   │
         │  Images)│   │  .NET API │  │  config.js  │
         └─────────┘   └─────┬─────┘  └─────────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
        ┌─────▼──────┐ ┌────▼────┐ ┌───────▼───────┐
        │  Secrets   │ │  RDS    │ │  CloudWatch   │
        │  Manager   │ │  SQL    │ │  Logs         │
        │  (DB creds)│ │  Server │ │               │
        └────────────┘ └─────────┘ └───────────────┘
```

**Request flow (user registration):**
```
Browser (S3 HTML page)
  → loads config.js (gets API URL)
  → POST http://<ECS_IP>:8080/api/offices/register
    → ECS Fargate (.NET 9 API)
      → Rate limiter: 5 req/min per IP
      → Model validation (BCrypt password rules, phone regex)
      → SQL: SELECT phone exists? → RDS SQL Server
      → SQL: INSERT INTO Offices → RDS SQL Server
      → CloudWatch: logs "Office registered: OfficeId=111"
  → 200 { success: true, officeId: 111 }
Browser displays Hebrew success banner
```

---

## Why Each Service

### GitHub Actions
- **Free** for public repos, generous minutes for private
- Native GitHub integration — triggers on push, PR, workflow completion
- Managed runners with pre-installed tools (Docker, AWS CLI, .NET)
- `workflow_run` trigger lets CD depend on CI completing without complex monorepo logic

### AWS ECR (Elastic Container Registry)
- Private Docker registry co-located with ECS — image pulls stay inside AWS network (fast, no egress cost)
- No authentication complexity — ECS tasks pull via IAM role, not Docker credentials
- Immutable image tags via SHA ensure exact reproducibility
- Lifecycle policies can auto-delete old images to control storage cost

### AWS ECS Fargate
- **Serverless containers** — no EC2 instances to manage, patch, or right-size
- Pay per vCPU/memory-second while the task runs — ideal for low-traffic apps
- `awsvpc` networking gives each task its own ENI and security group
- Rolling deployments with health checks built in
- CloudWatch log integration via `awslogs` driver — zero config

### AWS RDS SQL Server Express
- **Managed** — automated backups, minor version patching, monitoring
- SQL Server Express is free-tier eligible (up to 10GB)
- Private subnet placement — only reachable from ECS security group
- `db.t3.micro` costs ~$0.016/hr (~$12/month)
- EF Core migrations handle schema creation automatically on startup

### AWS Secrets Manager
- Encrypted at rest (AES-256) and in transit
- ECS injects secrets as environment variables at task start — connection string never appears in code, git, or task definition JSON (stored as `valueFrom` ARN reference)
- Automatic rotation support for future use
- IAM policy on `ecsTaskExecutionRole` scopes access to exactly one secret

### AWS CloudWatch Logs
- Every `ILogger` call from the .NET API appears here automatically via the `awslogs` driver
- Structured log format with timestamps and log levels
- Log stream per task (`api/api/<task-id>`) — makes per-deployment log isolation easy
- Retention policy (30 days) prevents unbounded storage growth
- Can trigger alarms on error patterns

### AWS S3 (Static Website Hosting)
- **Near-zero cost** for a few HTML/CSS/JS files
- No server needed — S3 serves files directly
- `aws s3 sync` in CD pipeline keeps frontend always in sync with latest ECS IP
- Global availability, high durability (11 nines)

---

## CI Pipeline — `ci.yml`

**Trigger:** Manual (`workflow_dispatch`) — prevents accidental deploys during debugging.

```
Trigger: Manual Run
         │
         ▼
    Job: test
    ├── actions/checkout@v4
    ├── Setup .NET 9
    ├── dotnet restore (test project)
    ├── dotnet build (Release)
    └── dotnet test (32 xUnit tests)
         │
         ▼ (on test success)
    Job: build-push
    ├── actions/checkout@v4
    ├── Configure AWS credentials
    ├── amazon-ecr-login@v2
    ├── docker build -f infrastructure/Dockerfile .
    │     (context excludes obj/bin via .dockerignore)
    ├── docker push :${{ github.sha }}
    └── docker push :latest
```

**Key points:**
- Tests run first — broken code never reaches ECR
- Two tags pushed: SHA (immutable, for exact rollback) and `latest` (convenience)
- `.dockerignore` excludes `obj/`, `bin/`, tests, docs — lean build context, forces clean recompile

---

## CD Pipeline — `cd.yml`

**Trigger:** Automatically when CI workflow completes successfully (`workflow_run`).

```
Trigger: CI success
         │
         ▼
    1. Checkout (at CI's commit SHA)
    2. Generate .env from GitHub Secrets
    3. Configure AWS credentials
    4. ECR login
         │
         ▼
    5. Fetch current task definition (aws ecs describe-task-definition)
    6. jq transform:
       - Set image → new SHA tag
       - Set ASPNETCORE_ENVIRONMENT env var
       - Set Cors__AllowedOrigin env var
       - Set secrets[0].valueFrom → Secrets Manager ARN (direct, not map())
       - Strip read-only fields (taskDefinitionArn, revision, etc.)
         │
         ▼
    7. Register new task definition revision
       - Capture new ARN → $new_task_def_arn
         │
         ▼
    8. aws ecs update-service --task-definition $new_task_def_arn --force-new-deployment
         │
         ▼
    9. Wait loop (15 min max, polls every 20s)
       - Checks: running == desired, pending == 0, deployments == 1
       - Live progress output every 20s
         │
         ▼
   10. Diagnose (runs only on failure)
       - Service state, stopped task reason, CloudWatch logs
         │
         ▼
   11. Discover new task public IP (ENI → Association.PublicIp)
         │
         ▼
   12. Health check (12 attempts × 10s)
       - GET /health → 200
         │
         ▼
   13. Verify DB migration in CloudWatch logs
       - Stream: api/api/<task-id> (derived from running task, not guessed)
       - Retry 6× × 5s for startup lag
       - Grep for "migration" lines
         │
         ▼
   14. Auto-generate config.js with new IP
   15. aws s3 sync frontend/ → S3 bucket
         │
         ▼
   16. Print deployment summary (URL, health, register endpoints)
```

**Key design decisions:**

| Decision | Why |
|---|---|
| Capture task def ARN explicitly | `--task-definition <name>` resolves to current latest — if registration fails silently, old revision redeploys and wait exits in 1 check |
| Custom wait loop vs `aws ecs wait` | Built-in waiter has 10-min hard cap with no progress output; custom loop shows live status and runs for 15 min |
| Direct secrets array assignment | `map(if .name == "..." then ...)` silently preserves bad values when condition mismatches |
| Derive log stream from task ID | `describe-log-streams --order-by LastEventTime` returns wrong stream during startup lag |
| Auto S3 sync in CD | ECS task IP changes on every deployment — frontend would break without it |

---

## EF Core Auto-Migration

On every ECS task startup, `Program.cs` runs:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration failed. Application cannot start.");
        throw;
    }
}
```

**Flow:**
1. EF Core reads `__EFMigrationsHistory` table (creates it if missing)
2. Compares applied migrations against assembly's `[Migration("...")]` classes
3. If `20260604000000_InitialCreate` not applied → runs `Up()`:
   - `CREATE SEQUENCE dbo.OfficeIdSequence START WITH 111`
   - `CREATE TABLE dbo.Offices (...)`
   - `CREATE UNIQUE INDEX IX_Offices_OfficeId`
   - `CREATE UNIQUE INDEX IX_Offices_Phone`
   - Inserts row into `__EFMigrationsHistory`
4. If already applied → skips (idempotent)
5. Logs result to CloudWatch

**Critical:** Both migration files must be committed:
- `20260604000000_InitialCreate.cs` — schema operations
- `20260604000000_InitialCreate.Designer.cs` — `[Migration("...")]` attribute

Without the designer file, EF Core finds the class but cannot identify it as a migration.

---

## GitHub Secrets Reference

| Secret | Value | Used In |
|---|---|---|
| `AWS_ACCESS_KEY_ID` | IAM user access key | CI + CD (ECR, ECS) |
| `AWS_SECRET_ACCESS_KEY` | IAM user secret key | CI + CD |
| `AWS_REGION` | `us-east-1` | All AWS commands |
| `ECR_REPOSITORY` | `investment-tracker-api` | CI (docker push) |
| `ECS_CLUSTER` | `investment-tracker-cluster` | CD |
| `ECS_SERVICE` | `investment-tracker-api` | CD |
| `ASPNETCORE_ENVIRONMENT` | `Production` | CD (task def env) |
| `CORS_ALLOWED_ORIGIN` | S3 website URL | CD (task def env) |
| `ECS_TASK_EXECUTION_ROLE_ARN` | `arn:aws:iam::648548511587:role/ecsTaskExecutionRole` | CD (task def) |

---

## IAM Roles & Permissions

### `ecsTaskExecutionRole` (used by ECS tasks at runtime)
- `AmazonECSTaskExecutionRolePolicy` (managed) — ECR pull, CloudWatch logs
- `secrets-manager-read` (inline) — `secretsmanager:GetSecretValue` on `investment-tracker/db-connection-*`

### GitHub Actions IAM User
Needs permissions for: ECR push, ECS describe/update, EC2 network interface describe, S3 sync, CloudWatch logs read.

---

## Known Limitations & Recommended Next Steps

| Limitation | Recommended Fix |
|---|---|
| ECS task IP changes on every deploy | Add **ALB** (~$16/month) for stable DNS name + HTTPS via ACM |
| API served over HTTP | ALB + ACM free certificate → HTTPS |
| S3 served over HTTP | Add **CloudFront** distribution in front of S3 |
| No staging environment | Add a `staging` branch + second ECS service |
| Manual CI trigger | Change `workflow_dispatch` to `push: branches: [main]` when stable |
| RDS costs $12/month always-on | Use Aurora Serverless v2 for pause-when-idle, or stop RDS manually when not testing |
