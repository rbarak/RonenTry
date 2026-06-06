# Investment Tax Tracking System — CPA Registration Portal

A web application for CPA offices to register and manage client investment information for tax reporting. Phase 1 implements CPA office registration with a Hebrew RTL interface.

**Stack:** HTML/CSS/JS frontend · C# .NET 9 Web API · SQL Server · Docker · AWS ECS Fargate

---

## Live URLs

| Resource | URL |
|---|---|
| Frontend | `http://investment-tracker-frontend-648548511587.s3-website-us-east-1.amazonaws.com` |
| API Health | `http://<ECS_IP>:8080/health` (IP changes per deployment — auto-updated by CD) |
| CloudWatch Logs | `/ecs/investment-tracker` log group in `us-east-1` |

---

## Local Development

### Prerequisites
- Docker Desktop

### Setup

```bash
git clone https://github.com/rbarak/RonenTry.git
cd RonenTry
cp infrastructure/.env.example infrastructure/.env
# Edit infrastructure/.env with your SA password
cd infrastructure
docker-compose up
```

API runs on `http://localhost:8080`. Open `frontend/index.html` in your browser.

---

## API Endpoints

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
{ "success": true, "officeId": 111, "message": "Office registered successfully" }
```

**Error `400`:**
```json
{ "success": false, "message": "מספר הטלפון כבר קיים במערכת" }
```

**Rate limit:** 5 requests/minute per IP → `429`

### `GET /health`
Returns `200 Healthy` — used by CD pipeline after every deployment.

---

## CI/CD Pipeline

| Workflow | Trigger | What it does |
|---|---|---|
| `ci.yml` | Manual (`workflow_dispatch`) | Build → 32 xUnit tests → Docker push to ECR (SHA + `latest`) |
| `cd.yml` | Auto after CI succeeds | Update task def → deploy → wait → health check → auto-update config.js → S3 sync |

See [CICD.md](CICD.md) for full architecture and pipeline explanation.

---

## Deployment Status (2026-06-07)

| Layer | Status | Notes |
|---|---|---|
| AWS infrastructure | ✅ Provisioned | All resources in `us-east-1` |
| S3 frontend | ✅ Live | Auto-synced by CD on every deploy |
| GitHub Secrets | ✅ Configured | All 9 secrets set |
| ECR image | ✅ Built | Task def revision 13, image `5077ded` |
| ECS task | ✅ Running | Health check passing |
| DB migration | ✅ Applied | `InitialCreate` — Offices table, sequence, indexes |
| Registration form | ✅ Working | Offices 111, 112, 113 registered |
| config.js auto-update | ✅ Automated | CD updates + syncs S3 on every deploy |

---

## Documentation

| File | Contents |
|---|---|
| [CICD.md](CICD.md) | Full architecture, why each AWS service, CI/CD flow |
| [database_connection.md](database_connection.md) | Connect to RDS from VS Code, useful queries |
| [Troubleshoot.md](Troubleshoot.md) | All issues encountered and fixes applied |
| [infrastructure/aws-deploy.md](infrastructure/aws-deploy.md) | AWS setup guide, resource IDs, lessons learned |
| [infrastructure/schema.sql](infrastructure/schema.sql) | Manual DB schema script (fallback if EF migration fails) |
| [spec.md](spec.md) | Full product specification — source of truth |
