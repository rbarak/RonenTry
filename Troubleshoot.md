# Investment Tracker — Full Troubleshooting Log

## Overview

Application components:

| Layer | Service |
|---|---|
| Frontend | AWS S3 Static Website (Hebrew RTL HTML/JS) |
| Backend | AWS ECS Fargate (.NET 9 Web API) |
| Database | AWS RDS SQL Server Express |
| Logs | AWS CloudWatch (`/ecs/investment-tracker`) |
| Secrets | AWS Secrets Manager |
| Images | AWS ECR |
| CI/CD | GitHub Actions (ci.yml + cd.yml) |

**Final result:** Registration form fully working. Offices 111, 112, 113 registered successfully.

---

## Issue 1 — ECR Image Not Found (`CannotPullContainerError`)

**Symptom:**
```
CannotPullContainerError: failed to resolve ref
648548511587.dkr.ecr.us-east-1.amazonaws.com/investment-tracker-api:latest
not found
```

**Root cause:** ECR repository was empty — CI had not run yet, so no `latest` image existed.

**Fix:** Trigger CI manually (GitHub Actions → ci.yml → Run workflow). CI builds the Docker image and pushes both a SHA-tagged image and the `latest` tag to ECR. Historical — does not recur after first CI run.

---

## Issue 2 — Secrets Manager `ValidationException: Invalid name`

**Symptom (ECS service events):**
```
ResourceInitializationError: unable to retrieve secret from asm:
failed to fetch secret arn:aws:secretsmanager:...:secret:investment-tracker/db-connection-KMHXlO
api error ValidationException: Invalid name. Must be a valid name containing
alphanumeric characters, or any of the following: -/_+=.@!
```

**Root cause:** The CD pipeline's jq transformation used a conditional `map()` to update the `valueFrom` field in the task definition secrets array. When the condition didn't match (empty array, name mismatch, or prior bad state), jq silently preserved the old/malformed value. Secrets Manager received a string it couldn't parse as a valid ARN.

**Broken jq (old):**
```jq
.containerDefinitions[0].secrets = (
  .containerDefinitions[0].secrets | map(
    if .name == "ConnectionStrings__DefaultConnection"
      then .valueFrom = "arn:aws:secretsmanager:..."
    else .
    end
  )
)
```

**Fix — direct array assignment in `cd.yml`:**
```jq
.containerDefinitions[0].secrets = [{"name": "ConnectionStrings__DefaultConnection", "valueFrom": "arn:aws:secretsmanager:us-east-1:648548511587:secret:investment-tracker/db-connection-KMHXlO"}]
```

---

## Issue 3 — SSM `AccessDeniedException`

**Symptom:**
```
ResourceInitializationError: unable to retrieve secrets from ssm:
AccessDeniedException: not authorized to perform: ssm:GetParameters
```

**Root cause:** ECS routes `valueFrom` to SSM Parameter Store when the value does NOT start with `arn:aws:secretsmanager:`. After the jq map() silently failed (Issue 2), a subsequent CD run wrote a non-ARN string into `valueFrom`, causing ECS to target SSM instead of Secrets Manager. The `ecsTaskExecutionRole` has no SSM permission (nor should it).

**Fix:** Same as Issue 2 — direct array assignment guarantees the correct ARN is always set.

**Verified working:**
```bash
aws ecs describe-task-definition --task-definition investment-tracker-api \
  --query 'taskDefinition.containerDefinitions[0].secrets'
# [{"name": "ConnectionStrings__DefaultConnection",
#   "valueFrom": "arn:aws:secretsmanager:us-east-1:648548511587:secret:investment-tracker/db-connection-KMHXlO"}]
```

---

## Issue 4 — `obj/` and `bin/` Committed to Git

**Symptom:** After `.dockerignore` was added (see Issue 6), the next CI build reported:
```
No migrations were found in assembly 'InvestmentTracker.Api'.
```
Previously the build silently worked because Docker was using a locally-compiled DLL.

**Root cause:** No `.gitignore` existed. `git add -A` had committed the entire `obj/` and `bin/` directories including compiled DLLs and the MSBuild `Investme.5EABC707.Up2Date` sentinel file. When Docker copied `backend/InvestmentTracker.Api/` into the build context, this sentinel made MSBuild believe everything was already compiled and skip recompilation — so it used the local DLL instead of rebuilding from source.

**Fix:**
1. Created `.gitignore` at repo root:
   ```
   bin/
   obj/
   ```
2. Created `.dockerignore` at repo root:
   ```
   backend/**/bin/
   backend/**/obj/
   backend/InvestmentTracker.Api.Tests/
   ```
3. Removed 498 tracked artifacts from git index:
   ```bash
   git rm -r --cached backend/InvestmentTracker.Api/bin/ backend/InvestmentTracker.Api/obj/ \
     backend/InvestmentTracker.Api.Tests/bin/ backend/InvestmentTracker.Api.Tests/obj/
   ```

---

## Issue 5 — Migration Designer File Not Committed

**Symptom:**
```
info: Microsoft.EntityFrameworkCore.Migrations[20406]
No migrations were found in assembly 'InvestmentTracker.Api'.
A migration needs to be added before the database can be updated.
```
Followed by SQL Error 208 on registration:
```
Invalid object name 'Offices'
```
Registration returns HTTP 500.

**Root cause:** EF Core requires **two** files per migration:
- `20260604000000_InitialCreate.cs` — the Up/Down schema operations
- `20260604000000_InitialCreate.Designer.cs` — the `[Migration("...")]` attribute EF Core uses to identify and register the migration class

Only the first file was committed. Without `[Migration("20260604000000_InitialCreate")]`, EF Core scans the assembly, finds `InitialCreate : Migration`, but cannot associate it with a migration ID — reports "no migrations found." `MigrateAsync()` completes without creating the `Offices` table.

The old Docker images appeared to work because they used the locally-compiled `obj/` DLL (from the developer's machine where the designer file existed locally). Once `.dockerignore` cleaned up `obj/`, the build was clean and the missing file was exposed.

**Fix:** Created `backend/InvestmentTracker.Api/Infrastructure/Migrations/20260604000000_InitialCreate.Designer.cs`:
```csharp
[DbContext(typeof(AppDbContext))]
[Migration("20260604000000_InitialCreate")]
partial class InitialCreate
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder) { ... }
}
```

**Verified in CloudWatch after next deploy:**
```
info: Microsoft.EntityFrameworkCore.Migrations[20402]
Applying migration '20260604000000_InitialCreate'.
CREATE TABLE [Offices] ...
CREATE SEQUENCE [dbo].[OfficeIdSequence] ...
info: Program[0]
Database migration completed successfully.
```

---

## Issue 6 — CD Deploy Reusing Old Task Definition ("1-second deploy")

**Symptom:** The CD pipeline's "Deploy to ECS Fargate" step appeared to complete in ~1 second and the ECS service did not update to the new image.

**Root cause:** The deploy step used the task definition NAME:
```bash
aws ecs update-service \
  --task-definition investment-tracker-api \   # resolves to "whatever is latest"
  --force-new-deployment
```
If `register-task-definition` failed silently, no new revision was created. `update-service` deployed the same existing revision, which ECS considered already stable — wait loop exited in one check.

**Fix:** Capture the new revision ARN explicitly from `register-task-definition` and pass it directly:
```yaml
NEW_TASK_DEF_ARN=$(aws ecs register-task-definition \
  --cli-input-json file://task-definition.json \
  --query 'taskDefinition.taskDefinitionArn' \
  --output text)

echo "new_task_def_arn=$NEW_TASK_DEF_ARN" >> $GITHUB_ENV

# In next step:
aws ecs update-service \
  --task-definition "$new_task_def_arn" \
  --force-new-deployment
```

---

## Issue 7 — Migration Verification Step: `exit code 1`

**Symptom:** Migration verification step correctly printed migration lines but then failed with `Error: Process completed with exit code 1`.

**Root cause:** The step ended with:
```bash
[ -z "$MIGRATION_LINES" ] && echo "⚠️ not found"
```
When `$MIGRATION_LINES` is NOT empty, `[ -z ... ]` returns exit code 1. Last command in step → step fails.

**Fix:**
```bash
if [ -z "$MIGRATION_LINES" ]; then
  echo "⚠️  Migration log lines not found after retries"
fi
```

---

## Issue 8 — Migration Log Stream: `ResourceNotFoundException`

**Symptom:**
```
Log stream: api/api/d5daf40afc9548f987fcaf41650eb7d1
None
Error: ResourceNotFoundException: The specified log stream does not exist.
```

**Root cause:** `describe-log-streams --order-by LastEventTime` returned a stream from an older task (new task stream had no events yet during startup).

**Fix:** Derive log stream name directly from running task ID and retry up to 6×:
```bash
TASK_ARN=$(aws ecs list-tasks --desired-status RUNNING ...)
TASK_ID="${TASK_ARN##*/}"
LOG_STREAM="api/api/$TASK_ID"

for i in 1 2 3 4 5 6; do
  MIGRATION_LINES=$(aws logs get-log-events --log-stream-name "$LOG_STREAM" ... | grep -i "migration" || true)
  [ -n "$MIGRATION_LINES" ] && break
  sleep 5
done
```

---

## Issue 9 — ECS Task IP Ephemeral (config.js Out of Date)

**Symptom:** Frontend shows `שגיאת חיבור לשרת` (Server connection error) after a redeployment.

**Root cause:** ECS Fargate assigns a new public IP to each task on every deployment or restart. `config.js` was pointing to the old IP.

**Fix — automated in `cd.yml`:**
```yaml
- name: Update frontend config.js with new ECS IP
  run: |
    cat > frontend/config.js <<EOF
    window.APP_CONFIG = {
      apiUrl: 'http://${{ steps.app-url.outputs.public_ip }}:8080/api/offices/register'
    };
    EOF

- name: Sync frontend to S3
  run: |
    aws s3 sync frontend/ s3://investment-tracker-frontend-648548511587/ --delete
```

---

## Final Verification (2026-06-07)

```
GET  /health                    → Healthy ✅
POST /api/offices/register      → {"success":true,"officeId":111} ✅
CloudWatch: "Office registered successfully: OfficeId=113" ✅
DB: Offices rows 111, 112, 113 confirmed via VS Code SQL extension ✅
```

---

## Quick Reference — Useful AWS CLI Commands

```powershell
$AWS = "C:\Program Files\Amazon\AWSCLIV2\aws.exe"

# Get current ECS task public IP
$TASK = & $AWS ecs list-tasks --cluster investment-tracker-cluster --service-name investment-tracker-api --desired-status RUNNING --query 'taskArns[0]' --output text --region us-east-1
$ENI  = & $AWS ecs describe-tasks --cluster investment-tracker-cluster --tasks $TASK --query 'tasks[0].attachments[0].details[?name==`networkInterfaceId`].value' --output text --region us-east-1
& $AWS ec2 describe-network-interfaces --network-interface-ids $ENI --query 'NetworkInterfaces[0].Association.PublicIp' --output text --region us-east-1

# Health check
curl.exe http://<IP>:8080/health

# View CloudWatch logs
& $AWS logs get-log-events --log-group-name /ecs/investment-tracker --log-stream-name "api/api/<TASK_ID>" --query 'events[*].message' --output text --region us-east-1

# Sync frontend to S3
& $AWS s3 sync frontend/ s3://investment-tracker-frontend-648548511587/ --delete --region us-east-1

# Check ECR images
& $AWS ecr describe-images --repository-name investment-tracker-api --region us-east-1 --query 'sort_by(imageDetails,&imagePushedAt)[-3:].{tags:imageTags,pushed:imagePushedAt}'

# Check task definition secrets
& $AWS ecs describe-task-definition --task-definition investment-tracker-api --region us-east-1 --query 'taskDefinition.containerDefinitions[0].secrets'
```
