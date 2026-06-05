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

## AWS Deployment

See [infrastructure/aws-deploy.md](infrastructure/aws-deploy.md) for step-by-step instructions to deploy to AWS ECS Fargate with RDS SQL Server and CloudFront-hosted frontend.

The CI/CD pipeline (`.github/workflows/deploy.yml`) triggers automatically on push to `main`.
