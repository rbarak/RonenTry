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

### Deployment Status (as of 2026-06-06, end of day)

| Layer | Status | Details |
|-------|--------|---------|
| AWS infrastructure | ✅ Provisioned | All resources in `us-east-1` (see `aws-deploy.md` for full list & console links) |
| S3 frontend bucket | ✅ Created & synced | `http://investment-tracker-frontend-648548511587.s3-website-us-east-1.amazonaws.com` |
| Code committed & pushed | ✅ Done | All changes on `main`; latest commit (22538c1) includes auto-migration + config.js |
| GitHub Secrets | ✅ Configured | All 9 secrets set |
| Docker image in ECR | ✅ Built | CI passed; image in ECR with SHA + `latest` tags |
| ECS task running | ✅ Live | Running at `34.227.223.172:8080` (health check: `/health`) |
| DB auto-migration | ⚠️ Pending | Ready in code; runs on next ECS startup (needs CI trigger to deploy) |
| Frontend form | ⚠️ Testing | Config.js updated with ECS IP; S3 synced; ready after next deployment |

**Next action:** 
1. Trigger CI manually (GitHub Actions → ci.yml → Run workflow)
2. Watch CD deploy (auto-runs after CI succeeds)
3. Open S3 URL and test the Hebrew registration form
4. Expected: success banner with `officeId ≥ 111`
