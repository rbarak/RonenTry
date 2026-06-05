# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Investment Tax Tracking System — Phase 1: CPA office registration portal. Hebrew RTL frontend (HTML/CSS/JS), C# .NET 9 Web API (Clean Architecture + Repository Pattern), SQL Server on AWS RDS, deployed via Docker to AWS ECS Fargate. The full specification is in `spec.md` — treat it as the single source of truth and ask no clarifying questions.

## Directory Structure

```
frontend/          # HTML5/CSS3/JS ES6+ — RTL Hebrew UI
backend/
  InvestmentTracker.Api/
    Controllers/   # HTTP layer only — no business logic
    Core/
      Entities/    # Domain models
      DTOs/        # Request/response shapes
      Interfaces/  # Repository contracts
    Infrastructure/
      Data/        # AppDbContext (EF Core)
      Repositories/
      Migrations/  # EF Core migrations
    Program.cs
infrastructure/    # Dockerfile, docker-compose.yml, .env.example, aws-deploy.md
.github/workflows/ # deploy.yml — CI/CD to AWS ECR + ECS Fargate
```

## Development Commands

**Local dev (full stack):**
```bash
cd infrastructure
docker-compose up
```
API runs on port `8080`; SQL Server on port `1433`.

**Backend only:**
```bash
cd backend/InvestmentTracker.Api
dotnet run
```

**Tests:**
```bash
cd backend
dotnet test
```

**EF Core migrations:**
```bash
cd backend/InvestmentTracker.Api
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## Backend Architecture

Clean Architecture — dependencies flow inward (Controllers → Core ← Infrastructure):
- Controllers call repository interfaces defined in `Core/Interfaces/`
- `Infrastructure/Repositories/` implements those interfaces against EF Core
- No direct DB calls in controllers; no business logic in repositories beyond CRUD

NuGet packages: `BCrypt.Net-Next` (password hashing), `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`.

## Key Constraints

- **Passwords:** BCrypt only — never store plain text. Minimum: 8 chars, 1 uppercase, 1 digit, 1 special char.
- **Phone:** unique in DB; client-side validation: 10–15 digits only.
- **UI language:** Hebrew RTL — `dir="rtl"` `lang="he"` on `<html>`. All error messages in Hebrew (see `spec.md` for exact strings).
- **CORS:** restrict to the known frontend origin (S3/CloudFront URL).
- **Rate limiting:** applied to `POST /api/offices/register`.
- **Security headers:** `X-Content-Type-Options`, `X-Frame-Options`, `Strict-Transport-Security` via middleware.
- **Audit logging:** log every registration attempt (success and failure) to CloudWatch.

## API

Single endpoint: `POST /api/offices/register` — request/response shapes in `spec.md`.

## AWS Environment (us-east-1, account 648548511587)

All infrastructure was provisioned on 2026-06-05. Use these IDs when working with AWS CLI or console.

| Resource | Name / ID |
|----------|-----------|
| IAM role (ECS tasks) | `ecsTaskExecutionRole` |
| ECS security group | `sg-0d2d51497d37f9c33` (inbound 8080) |
| RDS security group | `sg-00d6a98ed349a1079` (inbound 1433 from ECS SG only) |
| ECR repository | `648548511587.dkr.ecr.us-east-1.amazonaws.com/investment-tracker-api` |
| CloudWatch log group | `/ecs/investment-tracker` |
| ECS cluster | `investment-tracker-cluster` |
| ECS service | `investment-tracker-api` |
| ECS task definition | `investment-tracker-api:1` |
| RDS endpoint | `investment-tracker-db.cc9isgm6qkub.us-east-1.rds.amazonaws.com` |
| Secrets Manager | `investment-tracker/db-connection-KMHXlO` |

**Current state:** Code is written and reviewed but **not yet committed**. The ECS service is running with `desired-count=1` but will fail to start until a Docker image is pushed to ECR by GitHub Actions.

**To deploy:** Add GitHub Secrets → `git add . && git commit && git push` → GitHub Actions handles the rest.  
See `infrastructure/aws-deploy.md` Step 11 for the GitHub Secrets values.
