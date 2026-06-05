# AWS Deployment Guide

## Prerequisites
- AWS CLI v2 configured (`aws configure`)
- Docker installed and running
- An AWS account with appropriate IAM permissions

---

## 1. Create ECR Repository

```bash
aws ecr create-repository \
  --repository-name investment-tracker-api \
  --region <YOUR_REGION>
```

Note the `repositoryUri` from the output — you'll use it as the `ECR_REPOSITORY` GitHub secret.

---

## 2. Create RDS SQL Server

```bash
aws rds create-db-instance \
  --db-instance-identifier investment-tracker-db \
  --db-instance-class db.t3.micro \
  --engine sqlserver-ex \
  --master-username sa \
  --master-user-password "<STRONG_PASSWORD>" \
  --allocated-storage 20 \
  --license-model license-included \
  --no-multi-az \
  --region <YOUR_REGION>
```

Wait for the instance to become available:
```bash
aws rds wait db-instance-available --db-instance-identifier investment-tracker-db
```

Note the `Endpoint.Address` — use it in the connection string.

---

## 3. Store DB Credentials in AWS Secrets Manager

```bash
aws secretsmanager create-secret \
  --name investment-tracker/db-connection \
  --secret-string "Server=<RDS_ENDPOINT>,1433;Database=InvestmentTracker;User Id=sa;Password=<STRONG_PASSWORD>;TrustServerCertificate=True" \
  --region <YOUR_REGION>
```

---

## 4. Create ECS Fargate Cluster

```bash
aws ecs create-cluster \
  --cluster-name investment-tracker-cluster \
  --region <YOUR_REGION>
```

### Create Task Definition

Create `task-definition.json`:

```json
{
  "family": "investment-tracker-api",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "256",
  "memory": "512",
  "executionRoleArn": "arn:aws:iam::<ACCOUNT_ID>:role/ecsTaskExecutionRole",
  "containerDefinitions": [
    {
      "name": "api",
      "image": "<ECR_REPOSITORY_URI>:latest",
      "portMappings": [{ "containerPort": 8080, "protocol": "tcp" }],
      "environment": [
        { "name": "ASPNETCORE_ENVIRONMENT", "value": "Production" },
        { "name": "Cors__AllowedOrigin", "value": "https://<CLOUDFRONT_DOMAIN>" }
      ],
      "secrets": [
        {
          "name": "ConnectionStrings__DefaultConnection",
          "valueFrom": "arn:aws:secretsmanager:<REGION>:<ACCOUNT_ID>:secret:investment-tracker/db-connection"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/investment-tracker",
          "awslogs-region": "<YOUR_REGION>",
          "awslogs-stream-prefix": "api"
        }
      }
    }
  ]
}
```

Register the task definition:
```bash
aws ecs register-task-definition --cli-input-json file://task-definition.json
```

### Create ECS Service

```bash
aws ecs create-service \
  --cluster investment-tracker-cluster \
  --service-name investment-tracker-api \
  --task-definition investment-tracker-api \
  --desired-count 1 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[<SUBNET_ID>],securityGroups=[<SG_ID>],assignPublicIp=ENABLED}" \
  --region <YOUR_REGION>
```

---

## 5. Set Up S3 + CloudFront for Frontend

```bash
# Create S3 bucket
aws s3 mb s3://investment-tracker-frontend-<ACCOUNT_ID> --region <YOUR_REGION>

# Enable static website hosting
aws s3 website s3://investment-tracker-frontend-<ACCOUNT_ID> \
  --index-document index.html

# Upload frontend files
aws s3 sync frontend/ s3://investment-tracker-frontend-<ACCOUNT_ID>/ \
  --acl public-read
```

Create a CloudFront distribution pointing to the S3 bucket. Note the CloudFront domain and use it as `Cors__AllowedOrigin` in the ECS task definition.

---

## 6. Set Up CloudWatch Log Groups

```bash
aws logs create-log-group \
  --log-group-name /ecs/investment-tracker \
  --region <YOUR_REGION>

aws logs put-retention-policy \
  --log-group-name /ecs/investment-tracker \
  --retention-in-days 30
```

---

## 7. Configure GitHub Secrets

In your GitHub repository → Settings → Secrets and variables → Actions, add:

| Secret | Value |
|--------|-------|
| `AWS_ACCESS_KEY_ID` | IAM user access key |
| `AWS_SECRET_ACCESS_KEY` | IAM user secret key |
| `AWS_REGION` | e.g. `us-east-1` |
| `ECR_REPOSITORY` | ECR repository name (e.g. `investment-tracker-api`) |
| `ECS_CLUSTER` | `investment-tracker-cluster` |
| `ECS_SERVICE` | `investment-tracker-api` |

---

## 8. Run Database Migration

After the first deployment, run the EF Core migration against the RDS instance:

```bash
# From local machine with VPN/bastion access to RDS
cd backend/InvestmentTracker.Api
ConnectionStrings__DefaultConnection="Server=<RDS_ENDPOINT>,1433;..." dotnet ef database update
```

Or execute the migration SQL directly via SSMS / Azure Data Studio connected to RDS.
