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
