# Investment Tax Tracking System — CPA Office Registration Module

> **Instructions for Claude Code:**
> Generate a complete, production-ready solution for the registration module described below.
> Create ALL files listed in the Deliverables section with full, working code — no stubs, no placeholders.
> Follow the folder structure exactly. Ask no clarifying questions; use the spec as the single source of truth.

---

## Project Overview

A web application for CPA offices to collect and manage client investment information for tax reporting and planning.

**Phase 1 scope:** CPA office registration only.

**Platform support:** Windows, macOS, Android, iOS
**Primary UI language:** Hebrew (RTL — Right To Left)

---

## Folder Structure

```
/
├── frontend/
│   ├── index.html
│   ├── styles.css
│   └── app.js
├── backend/
│   └── InvestmentTracker.Api/   (.NET 9 Web API, Clean Architecture)
├── infrastructure/
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── .github/workflows/deploy.yml
└── README.md
```

---

## Technology Stack

| Layer          | Technology                                      |
| -------------- | ----------------------------------------------- |
| Frontend       | HTML5, CSS3, JavaScript ES6+                    |
| Backend        | C# .NET 9 Web API                               |
| ORM            | Entity Framework Core                           |
| Architecture   | Clean Architecture + Repository Pattern         |
| Database       | Microsoft SQL Server (AWS RDS)                  |
| Containerization | Docker + AWS ECR                              |
| Orchestration  | Docker Compose (local dev)                      |
| CI/CD          | GitHub Actions                                  |
| Secrets        | AWS Secrets Manager                             |
| Monitoring     | Amazon CloudWatch                               |
| Frontend Hosting | Amazon S3 + CloudFront CDN                   |
| Backend Hosting  | AWS ECS Fargate                              |

---

## Database Schema

### Table: `Offices`

```sql
CREATE TABLE Offices (
    ID               INT IDENTITY(1,1)   PRIMARY KEY,
    OfficeID         INT IDENTITY(111,1) NOT NULL UNIQUE,
    OfficeName       NVARCHAR(200)       NOT NULL,
    ManagerName      NVARCHAR(200)       NOT NULL,
    Email            NVARCHAR(255)       NOT NULL,
    Phone            NVARCHAR(20)        NOT NULL UNIQUE,
    UserName         NVARCHAR(100)       NOT NULL,
    PasswordHash     NVARCHAR(500)       NOT NULL,
    RegistrationDate DATETIME            NOT NULL DEFAULT GETUTCDATE()
);
```

**Constraints:**
- `Phone` must be unique across all rows
- `PasswordHash` must use BCrypt — never store plain text
- `RegistrationDate` is set server-side automatically

---

## API Specification

### `POST /api/offices/register`

**Request body:**
```json
{
  "officeName": "ABC CPA",
  "managerName": "David Cohen",
  "email": "david@abc.com",
  "phone": "0541234567",
  "userName": "abcuser",
  "password": "Password@123"
}
```

**Success response `200`:**
```json
{
  "success": true,
  "officeId": 111,
  "message": "Office registered successfully"
}
```

**Failure response `400`:**
```json
{
  "success": false,
  "message": "Phone number already exists"
}
```

---

## Frontend — Registration Page

### Page metadata
- `<title>`: הרשמה
- Direction: `dir="rtl"`, `lang="he"`

### Visual design
- Centered card layout on a **blue gradient** background
- Color palette: Blue primary, Light Gray accents, White surfaces
- Modern, professional — suited for a CPA firm
- Mobile-first, fully responsive

### Form fields

| # | Label (Hebrew) | Type     | SQL Field      | Icon | Required | Validation                                                      |
|---|---------------|----------|----------------|------|----------|-----------------------------------------------------------------|
| 1 | שם משרד        | text     | OfficeName     | 🏢   | ✅       | —                                                               |
| 2 | שם מנהל        | text     | ManagerName    | 👤   | ✅       | —                                                               |
| 3 | אימייל         | email    | Email          | ✉️   | ✅       | Regex — valid email format                                      |
| 4 | טלפון          | text     | Phone          | 📞   | ✅       | Numbers only, 10–15 digits, unique in DB                        |
| 5 | יוזר           | text     | UserName       | 👨‍💻   | ✅       | —                                                               |
| 6 | סיסמא         | password | PasswordHash   | 🔒   | ✅       | Min 8 chars, 1 uppercase, 1 digit, 1 special char               |

### Submit button
- Label: **הרשם**
- Style: Blue background, white text, rounded corners, hover animation, loading spinner while awaiting API
- Action: `POST /api/offices/register`

### Validation error messages (Hebrew)

| Field    | Error message |
|----------|---------------|
| Email    | `כתובת האימייל אינה תקינה` |
| Phone    | `מספר הטלפון חייב להכיל בין 10 ל-15 ספרות` |
| Password | `הסיסמא חייבת להכיל לפחות: אות גדולה אחת, ספרה אחת, תו מיוחד אחד, 8 תווים לפחות` |

---

## Backend — .NET 9 Web API

### Clean Architecture layers

```
InvestmentTracker.Api/
├── Controllers/
│   └── OfficesController.cs
├── Core/
│   ├── Entities/
│   │   └── Office.cs
│   ├── DTOs/
│   │   ├── RegisterOfficeRequest.cs
│   │   └── RegisterOfficeResponse.cs
│   └── Interfaces/
│       └── IOfficeRepository.cs
├── Infrastructure/
│   ├── Data/
│   │   └── AppDbContext.cs
│   ├── Repositories/
│   │   └── OfficeRepository.cs
│   └── Migrations/
└── Program.cs
```

### Requirements
- Use `BCrypt.Net-Next` for password hashing
- EF Core migrations for schema management
- Repository pattern — no direct DB calls in controllers
- `appsettings.json` + environment variable override for connection string
- CORS: allow frontend origin
- Rate limiting on `/api/offices/register`
- Secure HTTP headers middleware
- Audit logging for registration attempts

---

## Docker

### `infrastructure/Dockerfile` — Backend API

- Base image: `mcr.microsoft.com/dotnet/aspnet:9.0`
- Build image: `mcr.microsoft.com/dotnet/sdk:9.0`
- Multi-stage build
- Expose port `8080`

### `infrastructure/docker-compose.yml` — Local dev

Services:
1. `api` — builds from Dockerfile, port `8080:8080`
2. `sqlserver` — `mcr.microsoft.com/mssql/server:2022-latest`, port `1433:1433`

Environment variables via `.env` file (include `.env.example`).

---

## GitHub Actions CI/CD

**File:** `.github/workflows/deploy.yml`

Pipeline steps:
1. Trigger on push to `main`
2. Build Docker image
3. Run automated tests (`dotnet test`)
4. Push image to AWS ECR
5. Deploy to AWS ECS Fargate

Required GitHub Secrets:
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`
- `AWS_REGION`
- `ECR_REPOSITORY`
- `ECS_CLUSTER`
- `ECS_SERVICE`

---

## AWS Deployment

Provide a `infrastructure/aws-deploy.md` with step-by-step instructions to:
1. Create ECR repository
2. Create RDS SQL Server (db.t3.micro for MVP)
3. Create ECS Fargate cluster + task definition
4. Set up S3 bucket + CloudFront for frontend
5. Configure AWS Secrets Manager for DB credentials
6. Set up CloudWatch log groups

---

## Security Requirements

- All inputs validated server-side (not just client-side)
- CORS restricted to known frontend origin
- Rate limiting on registration endpoint
- BCrypt password hashing (never plain text)
- Secure HTTP response headers (`X-Content-Type-Options`, `X-Frame-Options`, `Strict-Transport-Security`)
- Audit log on every registration attempt (success and failure)

---

## README.md

Include:
- Project description
- Local dev setup (Docker Compose)
- Environment variable reference
- API endpoint summary
- AWS deployment overview link

---

## Deliverables Checklist

Generate every file below with **complete, working code**:

- [ ] `frontend/index.html` — RTL Hebrew registration page
- [ ] `frontend/styles.css` — Blue/gray/white responsive design
- [ ] `frontend/app.js` — Validation + API call + loading state
- [ ] `backend/InvestmentTracker.Api/` — Full .NET 9 Web API (all layers)
- [ ] `backend/InvestmentTracker.Api/Infrastructure/Migrations/` — EF Core migration
- [ ] `infrastructure/Dockerfile`
- [ ] `infrastructure/docker-compose.yml`
- [ ] `infrastructure/.env.example`
- [ ] `.github/workflows/deploy.yml`
- [ ] `infrastructure/aws-deploy.md`
- [ ] `README.md`
