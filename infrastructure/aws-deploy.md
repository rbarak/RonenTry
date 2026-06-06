# AWS Deployment Guide

## Provisioned Resources (2026-06-05)

> All infrastructure was created on 2026-06-05. Use these IDs directly — no need to re-run the provisioning steps.

**Account:** `648548511587` · **Region:** `us-east-1`

| Resource | Name / ID | AWS Console |
|----------|-----------|-------------|
| IAM role | `ecsTaskExecutionRole` | [IAM Roles](https://console.aws.amazon.com/iam/home#/roles/ecsTaskExecutionRole) |
| ECS security group | `sg-0d2d51497d37f9c33` | [EC2 SGs](https://console.aws.amazon.com/ec2/v2/home?region=us-east-1#SecurityGroups:) |
| RDS security group | `sg-00d6a98ed349a1079` | [EC2 SGs](https://console.aws.amazon.com/ec2/v2/home?region=us-east-1#SecurityGroups:) |
| ECR repository | `investment-tracker-api` | [ECR](https://console.aws.amazon.com/ecr/repositories/private/648548511587/investment-tracker-api?region=us-east-1) |
| CloudWatch log group | `/ecs/investment-tracker` | [CloudWatch](https://console.aws.amazon.com/cloudwatch/home?region=us-east-1#logsV2:log-groups/log-group/%2Fecs%2Finvestment-tracker) |
| ECS cluster | `investment-tracker-cluster` | [ECS](https://console.aws.amazon.com/ecs/v2/clusters/investment-tracker-cluster?region=us-east-1) |
| ECS service | `investment-tracker-api` | [ECS Service](https://console.aws.amazon.com/ecs/v2/clusters/investment-tracker-cluster/services/investment-tracker-api?region=us-east-1) |
| ECS task definition | `investment-tracker-api` (multiple revisions) | [Task Definitions](https://console.aws.amazon.com/ecs/v2/task-definitions/investment-tracker-api?region=us-east-1) |
| RDS instance | `investment-tracker-db` | [RDS](https://console.aws.amazon.com/rds/home?region=us-east-1#database:id=investment-tracker-db) |
| Secrets Manager | `investment-tracker/db-connection-KMHXlO` | [Secrets Manager](https://console.aws.amazon.com/secretsmanager/home?region=us-east-1#!/listSecrets) |
| S3 frontend bucket | `investment-tracker-frontend-648548511587` | [S3](https://console.aws.amazon.com/s3/buckets/investment-tracker-frontend-648548511587?region=us-east-1) |

**RDS endpoint:** `investment-tracker-db.cc9isgm6qkub.us-east-1.rds.amazonaws.com`

**ECR image URI:** `648548511587.dkr.ecr.us-east-1.amazonaws.com/investment-tracker-api`

**S3 frontend URL:** `http://investment-tracker-frontend-648548511587.s3-website-us-east-1.amazonaws.com`

**Secrets Manager ARN (hardcoded in cd.yml):** `arn:aws:secretsmanager:us-east-1:648548511587:secret:investment-tracker/db-connection-KMHXlO`

### GitHub Secrets — 9 required

Go to: https://github.com/rbarak/RonenTry/settings/secrets/actions

| Secret | Value | Status |
|--------|-------|--------|
| `AWS_ACCESS_KEY_ID` | Ronen IAM user access key | ✅ Set |
| `AWS_SECRET_ACCESS_KEY` | Ronen IAM user secret key | ✅ Set |
| `AWS_REGION` | `us-east-1` | ✅ Set |
| `ECR_REPOSITORY` | `investment-tracker-api` | ✅ Set |
| `ECS_CLUSTER` | `investment-tracker-cluster` | ✅ Set |
| `ECS_SERVICE` | `investment-tracker-api` | ✅ Set |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ✅ Set |
| `CORS_ALLOWED_ORIGIN` | `http://investment-tracker-frontend-648548511587.s3-website-us-east-1.amazonaws.com` | ✅ Set |
| `ECS_TASK_EXECUTION_ROLE_ARN` | `arn:aws:iam::648548511587:role/ecsTaskExecutionRole` | ✅ Set |

> `DB_SECRET_ARN` is **no longer a required secret** — the Secrets Manager ARN is hardcoded directly in `cd.yml` to avoid ARN formatting issues that caused ECS to route to SSM instead of Secrets Manager.

### Current deployment status (as of 2026-06-06, end of day)

| Step | Status | Notes |
|------|--------|-------|
| AWS infrastructure | ✅ Provisioned | All resources created in `us-east-1` |
| S3 frontend bucket | ✅ Synced | Files uploaded; config.js points to `34.227.223.172:8080` |
| GitHub Secrets | ✅ Configured | All 9 secrets set (see table below) |
| Code committed & pushed | ✅ Done | Latest commit (22538c1) includes auto-migration + config.js |
| CI pipeline | ✅ Working | 32 tests pass; Docker image in ECR with SHA + `latest` tags |
| CD pipeline | ✅ Deployed | ECS task running at `34.227.223.172:8080` |
| Health check | ✅ Passing | `GET /health` returns `200 Healthy` |
| DB auto-migration | ⚠️ Ready | Code in place; runs on next ECS startup (needs CI trigger to deploy) |
| Registration form | ⚠️ Ready for testing | Config.js updated, S3 synced; test after next deployment |

### GitHub Secrets Configuration (9 required)

Go to: https://github.com/rbarak/RonenTry/settings/secrets/actions

| Secret | Value | Purpose | Status |
|--------|-------|---------|--------|
| `AWS_ACCESS_KEY_ID` | Ronen IAM user access key | ECR login + ECS deploy | ✅ Set |
| `AWS_SECRET_ACCESS_KEY` | Ronen IAM user secret key | ECR login + ECS deploy | ✅ Set |
| `AWS_REGION` | `us-east-1` | All AWS CLI commands | ✅ Set |
| `ECR_REPOSITORY` | `investment-tracker-api` | Docker image name in ECR | ✅ Set |
| `ECS_CLUSTER` | `investment-tracker-cluster` | ECS cluster name | ✅ Set |
| `ECS_SERVICE` | `investment-tracker-api` | ECS service name | ✅ Set |
| `ASPNETCORE_ENVIRONMENT` | `Production` | App environment setting | ✅ Set |
| `CORS_ALLOWED_ORIGIN` | `http://investment-tracker-frontend-648548511587.s3-website-us-east-1.amazonaws.com` | Frontend origin (CORS) | ✅ Set |
| `ECS_TASK_EXECUTION_ROLE_ARN` | `arn:aws:iam::648548511587:role/ecsTaskExecutionRole` | ECS task IAM role | ✅ Set |

**Note:** `DB_SECRET_ARN` is NOT required — the Secrets Manager ARN is hardcoded in `cd.yml` to avoid formatting issues that caused routing to SSM instead of Secrets Manager.

### Cost note

~$0.016/hr for RDS db.t3.micro (main cost). Delete after POC:
```bash
aws rds delete-db-instance --db-instance-identifier investment-tracker-db --skip-final-snapshot --region us-east-1
```

### Known issues / lessons learned (2026-06-06)

**ECS `valueFrom` routing** (CRITICAL)
- A plain secret name (no `arn:` prefix) routes to **SSM Parameter Store**, not Secrets Manager
- ECS interprets `arn:aws:secretsmanager:...` → Secrets Manager; anything else → SSM
- Fix: Use full ARN — now hardcoded in `cd.yml` as `arn:aws:secretsmanager:us-east-1:648548511587:secret:investment-tracker/db-connection-KMHXlO`

**jq `$ENV` reserved built-in** (CRITICAL)
- `$ENV` is a jq global containing the entire process environment as JSON
- `--arg ENV "value"` does NOT reliably override it on all jq versions
- Fix: Never use `ENV` as a jq variable name. Use `$DOTNET_ENV`, `$CORS_ORIGIN`, `$ROLE_ARN`, `$SECRET_ARN` instead

**ECS task public IP is ephemeral**
- IP changes on every task replacement (redeployment)
- After each CD run, update `frontend/config.js` with the new IP and re-sync S3
- For production: add an ALB (Application Load Balancer) for a stable DNS endpoint

**CD wait loop improvements** (2026-06-06)
- Replaced `aws ecs wait services-stable` with custom polling loop
- Reason: built-in waiter has 10-minute hard timeout with no visibility into why tasks fail
- Fix: custom loop polls every 20 seconds, shows progress, has 15-min timeout
- Added `Diagnose deployment failure` step that runs on any failure and prints stopped task reason + CloudWatch logs

**S3 frontend requires manual sync after config changes**
- S3 static hosting works, but no CI/CD integration yet
- After updating `frontend/config.js` with new ECS IP, must manually run: `aws s3 sync frontend/ s3://investment-tracker-frontend-648548511587/`
- Future: add S3 sync step to CD workflow

**RDS is in private subnet (no direct local access)**
- Windows machine cannot reach RDS directly (security group restricts inbound to ECS SG only)
- DB migration must run from ECS container startup (auto-migrates with `db.Database.MigrateAsync()`)
- To manually run migrations from local: temporarily open RDS security group OR use SSM Session Manager to exec into ECS task

**DB auto-migration approach**
- `Program.cs` calls `db.Database.MigrateAsync()` before `app.Run()`
- EF Core checks `__EFMigrationsHistory` table and skips if migration already applied
- Safe for every restart; no risk of duplicate table creation
- Requires migrations to be compiled into the assembly (not separately managed) — currently `InitialCreate` is compiled in

---

## Next Steps for Tomorrow (2026-06-07)

### Immediate (5 minutes)
1. **Trigger CI manually:** https://github.com/rbarak/RonenTry/actions/workflows/ci.yml → "Run workflow" → select `main` → "Run"
2. **Watch CD auto-deploy** (~5 minutes): CI will trigger CD automatically
   - CD will create new ECS task with updated code
   - `db.Database.MigrateAsync()` runs on task startup
   - New task public IP will be different — check the CD log output

### Short-term (15 minutes)
3. **Test the registration form:**
   - Open: `http://investment-tracker-frontend-648548511587.s3-website-us-east-1.amazonaws.com`
   - Fill in Hebrew form with valid data
   - Expected response: success banner with `officeId ≥ 111`
   - Check CloudWatch logs if there's an error: https://console.aws.amazon.com/cloudwatch/home?region=us-east-1#logsV2:log-groups/log-group/%2Fecs%2Finvestment-tracker

### Known gotchas to watch for
- If form shows "Server connection error" (`שגיאת חיבור לשרת`): DB migration may not have run yet (check ECS task startup logs)
- If deployment times out: check "Diagnose deployment failure" section in CD log for the actual error
- If ECS IP changed: you may need to update `frontend/config.js` and re-sync S3 (see ECS Service → Task page for new IP)

### After testing works
- Delete RDS (costs ~$1.10/day): `aws rds delete-db-instance --db-instance-identifier investment-tracker-db --skip-final-snapshot --region us-east-1`
- Or keep running for continued testing

---

## Architecture Overview

```
                        ┌─────────────────────────────────────┐
                        │           AWS Cloud                  │
  Browser               │                                      │
    │                   │  ┌──────────────┐   ┌─────────────┐ │
    ├──(HTML/CSS/JS)────►  │  CloudFront  ├──►│  S3 Bucket  │ │
    │                   │  │  (CDN/HTTPS) │   │  (frontend) │ │
    │                   │  └──────────────┘   └─────────────┘ │
    │                   │                                      │
    │                   │  ┌──────────────┐   ┌─────────────┐ │
    └──(API calls)──────►  │  ECS Fargate ├──►│  RDS SQL    │ │
                        │  │  .NET 9 API  │   │  Server     │ │
                        │  │  port 8080   │   │  port 1433  │ │
                        │  └──────┬───────┘   └─────────────┘ │
                        │         │                            │
                        │  ┌──────▼───────┐   ┌─────────────┐ │
                        │  │  Secrets Mgr │   │  CloudWatch │ │
                        │  │  (DB creds)  │   │    Logs     │ │
                        │  └──────────────┘   └─────────────┘ │
                        └─────────────────────────────────────┘
```

**MVP note:** For MVP the ECS task uses `assignPublicIp=ENABLED` (direct public IP, no ALB). The API URL changes if the task is replaced — update `frontend/config.js` and re-upload when it does. Add an ALB when stability is needed.

---

## Prerequisites

- AWS CLI v2 installed and configured (`aws configure`)
- Docker Desktop installed and running
- .NET 9 SDK (for running the EF Core database migration)
- An IAM user with administrator access for the initial one-time setup

Replace every `<PLACEHOLDER>` below before running commands.

---

## Step 1 — IAM Setup

This is the most common deployment blocker. Create two identities before doing anything else.

### 1a. GitHub Actions IAM User

Create a dedicated user with programmatic access only (no console login):

```bash
aws iam create-user --user-name investment-tracker-ci

aws iam create-access-key --user-name investment-tracker-ci
# Save the AccessKeyId and SecretAccessKey — you only see the secret once
```

Attach an inline policy granting the minimum permissions the pipeline needs:

```bash
aws iam put-user-policy \
  --user-name investment-tracker-ci \
  --policy-name investment-tracker-ci-policy \
  --policy-document '{
    "Version": "2012-10-17",
    "Statement": [
      {
        "Effect": "Allow",
        "Action": ["ecr:GetAuthorizationToken"],
        "Resource": "*"
      },
      {
        "Effect": "Allow",
        "Action": [
          "ecr:BatchCheckLayerAvailability",
          "ecr:GetDownloadUrlForLayer",
          "ecr:BatchGetImage",
          "ecr:InitiateLayerUpload",
          "ecr:UploadLayerPart",
          "ecr:CompleteLayerUpload",
          "ecr:PutImage"
        ],
        "Resource": "arn:aws:ecr:<YOUR_REGION>:<YOUR_ACCOUNT_ID>:repository/investment-tracker-api"
      },
      {
        "Effect": "Allow",
        "Action": [
          "ecs:UpdateService",
          "ecs:DescribeServices"
        ],
        "Resource": "arn:aws:ecs:<YOUR_REGION>:<YOUR_ACCOUNT_ID>:service/investment-tracker-cluster/investment-tracker-api"
      }
    ]
  }'
```

The `AccessKeyId` and `SecretAccessKey` from the `create-access-key` output become the `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` GitHub Secrets (Step 10).

### 1b. ECS Task Execution Role

This role lets the ECS task pull the Docker image and read secrets at runtime:

```bash
# Create the role
aws iam create-role \
  --role-name ecsTaskExecutionRole \
  --assume-role-policy-document '{
    "Version": "2012-10-17",
    "Statement": [{
      "Effect": "Allow",
      "Principal": {"Service": "ecs-tasks.amazonaws.com"},
      "Action": "sts:AssumeRole"
    }]
  }'

# Attach AWS managed policy (ECR pull + CloudWatch logs)
aws iam attach-role-policy \
  --role-name ecsTaskExecutionRole \
  --policy-arn arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy

# Attach inline policy to allow reading the DB secret from Secrets Manager
aws iam put-role-policy \
  --role-name ecsTaskExecutionRole \
  --policy-name secrets-manager-read \
  --policy-document '{
    "Version": "2012-10-17",
    "Statement": [{
      "Effect": "Allow",
      "Action": ["secretsmanager:GetSecretValue"],
      "Resource": "arn:aws:secretsmanager:<YOUR_REGION>:<YOUR_ACCOUNT_ID>:secret:investment-tracker/db-connection-*"
    }]
  }'
```

---

## Step 2 — Security Groups

Create security groups before creating RDS or ECS, so you can reference them by ID.

```bash
# Get your default VPC ID
VPC_ID=$(aws ec2 describe-vpcs --filters Name=isDefault,Values=true \
  --query 'Vpcs[0].VpcId' --output text)

# Security group for ECS API tasks
SG_ECS=$(aws ec2 create-security-group \
  --group-name sg-investment-tracker-api \
  --description "ECS API inbound 8080" \
  --vpc-id $VPC_ID \
  --query GroupId --output text)

aws ec2 authorize-security-group-ingress \
  --group-id $SG_ECS \
  --protocol tcp --port 8080 --cidr 0.0.0.0/0

# Security group for RDS — only reachable from the ECS SG
SG_RDS=$(aws ec2 create-security-group \
  --group-name sg-investment-tracker-rds \
  --description "RDS SQL Server inbound 1433 from ECS only" \
  --vpc-id $VPC_ID \
  --query GroupId --output text)

aws ec2 authorize-security-group-ingress \
  --group-id $SG_RDS \
  --protocol tcp --port 1433 \
  --source-group $SG_ECS

echo "ECS SG: $SG_ECS"
echo "RDS SG: $SG_RDS"
# Note both IDs — you'll need them in later steps
```

---

## Step 3 — ECR Repository

```bash
aws ecr create-repository \
  --repository-name investment-tracker-api \
  --region <YOUR_REGION>
```

The output includes `repositoryUri` in the format:
`<ACCOUNT_ID>.dkr.ecr.<REGION>.amazonaws.com/investment-tracker-api`

The **repository name** (`investment-tracker-api`) is what goes in the `ECR_REPOSITORY` GitHub Secret — not the full URI. The full URI prefix is resolved automatically by the `amazon-ecr-login` GitHub Action.

---

## Step 4 — RDS SQL Server

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
  --vpc-security-group-ids <RDS_SG_ID> \
  --region <YOUR_REGION>
```

Wait for the instance to become available (~10 min):

```bash
aws rds wait db-instance-available --db-instance-identifier investment-tracker-db

# Get the endpoint
aws rds describe-db-instances \
  --db-instance-identifier investment-tracker-db \
  --query 'DBInstances[0].Endpoint.Address' \
  --output text
```

Note the endpoint address — you'll use it in the next step.

---

## Step 5 — Store DB Credentials in Secrets Manager

```bash
aws secretsmanager create-secret \
  --name investment-tracker/db-connection \
  --secret-string "Server=<RDS_ENDPOINT>,1433;Database=InvestmentTracker;User Id=sa;Password=<STRONG_PASSWORD>;TrustServerCertificate=True" \
  --region <YOUR_REGION>
```

Note the full secret ARN from the output — it goes into the ECS task definition.

---

## Step 6 — CloudWatch Log Group

Create the log group before the ECS task definition references it:

```bash
aws logs create-log-group \
  --log-group-name /ecs/investment-tracker \
  --region <YOUR_REGION>

aws logs put-retention-policy \
  --log-group-name /ecs/investment-tracker \
  --retention-in-days 30
```

---

## Step 7 — ECS Fargate Cluster, Task Definition & Service

### Create the cluster

```bash
aws ecs create-cluster \
  --cluster-name investment-tracker-cluster \
  --region <YOUR_REGION>
```

### Register the task definition

Save the file below as `task-definition.json` (replace all placeholders):

```json
{
  "family": "investment-tracker-api",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "256",
  "memory": "512",
  "executionRoleArn": "arn:aws:iam::<YOUR_ACCOUNT_ID>:role/ecsTaskExecutionRole",
  "containerDefinitions": [
    {
      "name": "api",
      "image": "<YOUR_ACCOUNT_ID>.dkr.ecr.<YOUR_REGION>.amazonaws.com/investment-tracker-api:latest",
      "portMappings": [{ "containerPort": 8080, "protocol": "tcp" }],
      "environment": [
        { "name": "ASPNETCORE_ENVIRONMENT", "value": "Production" },
        { "name": "Cors__AllowedOrigin", "value": "https://<YOUR_CLOUDFRONT_DOMAIN>" }
      ],
      "secrets": [
        {
          "name": "ConnectionStrings__DefaultConnection",
          "valueFrom": "arn:aws:secretsmanager:<YOUR_REGION>:<YOUR_ACCOUNT_ID>:secret:investment-tracker/db-connection-<SUFFIX>"
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

> The `<SUFFIX>` in the secret ARN is the random 6-character suffix AWS appends (visible in the full ARN from Step 5).

```bash
aws ecs register-task-definition --cli-input-json file://task-definition.json
```

### Get a subnet ID from your default VPC

```bash
SUBNET_ID=$(aws ec2 describe-subnets \
  --filters Name=defaultForAz,Values=true \
  --query 'Subnets[0].SubnetId' --output text)
echo $SUBNET_ID
```

### Create the ECS service

```bash
aws ecs create-service \
  --cluster investment-tracker-cluster \
  --service-name investment-tracker-api \
  --task-definition investment-tracker-api \
  --desired-count 1 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[$SUBNET_ID],securityGroups=[$SG_ECS],assignPublicIp=ENABLED}" \
  --region <YOUR_REGION>
```

### Get the task's public IP (needed for frontend config)

```bash
TASK_ARN=$(aws ecs list-tasks \
  --cluster investment-tracker-cluster \
  --service-name investment-tracker-api \
  --query 'taskArns[0]' --output text)

ENI_ID=$(aws ecs describe-tasks \
  --cluster investment-tracker-cluster \
  --tasks $TASK_ARN \
  --query 'tasks[0].attachments[0].details[?name==`networkInterfaceId`].value' \
  --output text)

aws ec2 describe-network-interfaces \
  --network-interface-ids $ENI_ID \
  --query 'NetworkInterfaces[0].Association.PublicIp' \
  --output text
```

Note this IP — you'll use it in Step 9 when updating `config.js`.

---

## Step 8 — Run Database Migration

The EF Core migration must run once against the RDS instance before any registrations can succeed.

**Option A — from your local machine** (requires network access to RDS port 1433):

```bash
cd backend/InvestmentTracker.Api
ConnectionStrings__DefaultConnection="Server=<RDS_ENDPOINT>,1433;Database=InvestmentTracker;User Id=sa;Password=<STRONG_PASSWORD>;TrustServerCertificate=True" \
  dotnet ef database update
```

**Option B — via ECS Exec** (no local network access needed):

```bash
# Enable ECS Exec on the service first
aws ecs update-service \
  --cluster investment-tracker-cluster \
  --service investment-tracker-api \
  --enable-execute-command

# Then exec into the running task and run the migration
aws ecs execute-command \
  --cluster investment-tracker-cluster \
  --task <TASK_ARN> \
  --container api \
  --interactive \
  --command "/bin/sh"
# Inside the container: dotnet ef database update (requires EF tools installed in image)
```

**Option C — generate and run SQL directly**:

```bash
cd backend/InvestmentTracker.Api
dotnet ef migrations script --output migration.sql
# Apply migration.sql to RDS via SSMS or Azure Data Studio
```

---

## Step 9 — S3 + CloudFront for Frontend

### Create the S3 bucket

```bash
# us-east-1 does not use --create-bucket-configuration; other regions do
aws s3api create-bucket \
  --bucket investment-tracker-frontend-<YOUR_ACCOUNT_ID> \
  --region <YOUR_REGION> \
  --create-bucket-configuration LocationConstraint=<YOUR_REGION>

# Enable static website hosting
aws s3 website s3://investment-tracker-frontend-<YOUR_ACCOUNT_ID> \
  --index-document index.html
```

### Allow public read via bucket policy (not ACLs — ACLs are disabled on new accounts)

```bash
# Disable the "block all public access" setting first
aws s3api put-public-access-block \
  --bucket investment-tracker-frontend-<YOUR_ACCOUNT_ID> \
  --public-access-block-configuration \
    "BlockPublicAcls=false,IgnorePublicAcls=false,BlockPublicPolicy=false,RestrictPublicBuckets=false"

# Apply a bucket policy granting public read
aws s3api put-bucket-policy \
  --bucket investment-tracker-frontend-<YOUR_ACCOUNT_ID> \
  --policy '{
    "Version": "2012-10-17",
    "Statement": [{
      "Effect": "Allow",
      "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::investment-tracker-frontend-<YOUR_ACCOUNT_ID>/*"
    }]
  }'
```

### Create a CloudFront distribution

```bash
aws cloudfront create-distribution \
  --origin-domain-name investment-tracker-frontend-<YOUR_ACCOUNT_ID>.s3-website-<YOUR_REGION>.amazonaws.com \
  --default-root-object index.html
```

Note the `DomainName` from the output (e.g. `d1abc2def3gh4i.cloudfront.net`). This is your frontend URL and the value for `Cors__AllowedOrigin` in the ECS task definition.

---

## Step 10 — Update Frontend API URL for Production

Before uploading the frontend to S3, update `frontend/config.js` with the ECS task's public IP from Step 7:

```js
window.APP_CONFIG = {
  apiUrl: 'http://<ECS_PUBLIC_IP>:8080/api/offices/register'
};
```

Then upload all frontend files:

```bash
aws s3 sync frontend/ s3://investment-tracker-frontend-<YOUR_ACCOUNT_ID>/
```

> Each time the ECS task is replaced (e.g. after a deployment), AWS assigns a new public IP. Update `config.js` and re-sync to S3 after every deployment until you add an ALB with a stable DNS name.

---

## Step 11 — Configure GitHub Secrets

Go to **GitHub repo → Settings → Secrets and variables → Actions → New repository secret** and add:

| Secret | Where to find it | Example |
|--------|-----------------|---------|
| `AWS_ACCESS_KEY_ID` | IAM → Users → investment-tracker-ci → Security credentials | `AKIAIOSFODNN7EXAMPLE` |
| `AWS_SECRET_ACCESS_KEY` | Shown once when you ran `create-access-key` in Step 1 | `wJalrXUtnFEMI/K7MDENG/...` |
| `AWS_REGION` | Your chosen AWS region | `us-east-1` |
| `ECR_REPOSITORY` | ECR **repository name only** — not the full URI | `investment-tracker-api` |
| `ECS_CLUSTER` | ECS cluster name | `investment-tracker-cluster` |
| `ECS_SERVICE` | ECS service name | `investment-tracker-api` |

**Important:** `ECR_REPOSITORY` is the short name only (`investment-tracker-api`). The full image URI (`<account>.dkr.ecr.<region>.amazonaws.com/investment-tracker-api`) is assembled automatically in the workflow using the registry output from `amazon-ecr-login`.

---

## GitHub Actions Workflow Summary

The pipeline in `.github/workflows/deploy.yml` triggers on every push to `main`:

| Job | Steps |
|-----|-------|
| `build-test` | Checkout → Setup .NET 9 → Restore → Build → Run xUnit tests |
| `deploy` (runs only if tests pass) | Checkout → Configure AWS credentials → Login to ECR → `docker build` → `docker push` → `ecs update-service --force-new-deployment` → Wait for service to stabilize |

---

## End-to-End Verification

After completing all steps and pushing to `main`:

1. **GitHub Actions** — all workflow steps should be green in the Actions tab
2. **API health check**:
   ```bash
   curl -s -o /dev/null -w "%{http_code}" \
     http://<ECS_PUBLIC_IP>:8080/api/offices/register \
     -X POST -H "Content-Type: application/json" \
     -d '{"officeName":"Test","managerName":"Test","email":"t@t.com","phone":"0541234567","userName":"u","password":"Pass@123"}'
   # Expected: 200
   ```
3. **Frontend** — open `https://<CLOUDFRONT_DOMAIN>` in a browser, verify the Hebrew form loads, submit a valid registration, and confirm the success message shows the office ID (starting at 111)
4. **CloudWatch** — confirm logs appear in `/ecs/investment-tracker` log group
