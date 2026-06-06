# Investment Tax Tracking System — CPA Registration Portal

A web application for CPA offices to register and manage client investment information for tax reporting. Phase 1 implements CPA office registration with a Hebrew RTL interface.

**Stack:** HTML/CSS/JS frontend · C# .NET 9 Web API · SQL Server · Docker · AWS ECS Fargate

---

## Local Development

### Prerequisites
- Docker Desktop

### Setup

```bash
# Clone the repository
git clone https://github.com/RonenBarak/RonenTry.git
cd RonenTry

# Copy environment file
cp infrastructure/.env.example infrastructure/.env
# Edit infrastructure/.env with your preferred SA password

# Start all services
cd infrastructure
docker-compose up
```

The API will be available at `http://localhost:8080`.  
Open `frontend/index.html` in your browser to use the registration form.

### Run migrations (first time)

```bash
cd backend/InvestmentTracker.Api
dotnet ef database update
```

---

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `SA_PASSWORD` | SQL Server SA password | — |
| `ConnectionStrings__DefaultConnection` | Full ADO.NET connection string | see `.env.example` |
| `CORS_ALLOWED_ORIGIN` | Allowed frontend origin for CORS | `http://localhost` |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment | `Development` |

---

## API Endpoint

### `POST /api/offices/register`

**Request:**
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

**Success `200`:**
```json
{
  "success": true,
  "officeId": 111,
  "message": "Office registered successfully"
}
```

**Error `400`:**
```json
{
  "success": false,
  "message": "מספר הטלפון כבר קיים במערכת"
}
```

**Rate limit:** 5 requests per minute per IP → `429 Too Many Requests`

---

## CI/CD Pipeline

Two separate workflows in `.github/workflows/`:

| Workflow | Trigger | What it does |
|----------|---------|-------------|
| `ci.yml` | Manual (`workflow_dispatch`) | Build → test (32 xUnit tests) → push Docker image to ECR (SHA + `latest` tags) |
| `cd.yml` | Auto after CI succeeds | Generate `.env` from Secrets → update ECS task definition → deploy → health check → print live URL |
| `deploy.yml` | Disabled | Legacy — replaced by ci.yml + cd.yml |

## AWS Deployment

See [infrastructure/aws-deploy.md](infrastructure/aws-deploy.md) for step-by-step setup guide and full resource reference.

### Deployment Status (as of 2026-06-06)

| Layer | Status | Notes |
|-------|--------|-------|
| AWS infrastructure | ✅ Provisioned | All resources in `us-east-1` — see `aws-deploy.md` |
| S3 frontend bucket | ✅ Created | `http://investment-tracker-frontend-648548511587.s3-website-us-east-1.amazonaws.com` |
| Code committed & pushed | ✅ Done | All commits on `main` branch |
| GitHub Secrets | ✅ Configured | 9 secrets set (see `aws-deploy.md` for full list) |
| Docker image in ECR | ✅ Pushed | CI pipeline succeeded; image tagged with SHA + `latest` |
| ECS task running | ⚠️ Pending | CD pipeline being debugged — trigger CI to verify latest fix |
| DB migration | ❌ Pending | Run once ECS task is healthy |
| `frontend/config.js` updated | ❌ Pending | Update with ECS task public IP after first successful CD run |

**Next action:** Trigger CI manually → watch CD → get ECS IP → update config.js → run migration.
