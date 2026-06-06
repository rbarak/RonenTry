# Investment Tracker - Deployment Troubleshooting Log

## Overview

Application components:

* Frontend: AWS S3 Static Website Hosting
* Backend: AWS ECS Fargate (.NET API)
* Database: AWS RDS
* Logs: CloudWatch
* Secrets: AWS Secrets Manager

---

# Initial Issue

Frontend website loaded successfully from S3, but form submission failed with:

```text
Server connection error. Please try again later.
```

---

# Investigation Steps

## 1. Verify AWS Account

Checked active AWS account:

```bash
aws sts get-caller-identity
```

Confirmed deployment account:

```text
648548511587
```

---

## 2. Update Frontend API Endpoint

Original config.js:

```javascript
window.APP_CONFIG = {
  apiUrl: 'http://34.227.223.172:8080/api/offices/register'
};
```

Updated to:

```javascript
window.APP_CONFIG = {
  apiUrl: 'http://50.17.14.205:8080/api/offices/register'
};
```

---

## 3. Upload Frontend to S3

Synced frontend files:

```bash
aws s3 sync frontend/ s3://investment-tracker-frontend-648548511587/ --delete
```

Upload succeeded:

```text
upload: frontend\styles.css
upload: frontend\app.js
upload: frontend\index.html
upload: frontend\config.js
```

---

## 4. Verify Backend Health

Executed:

```bash
curl.exe http://50.17.14.205:8080/health
```

Response:

```text
Healthy
```

Result:

* ECS task reachable
* Security Group working
* Backend running correctly

---

## 5. Browser Console Investigation

Observed error:

```text
Access to fetch at
'http://50.17.14.205:8080/api/offices/register'
from origin
'http://investment-tracker-frontend-648548511587.s3-website-us-east-1.amazonaws.com'
has been blocked by CORS policy
```

Initial assumption:

* CORS configuration issue

---

## 6. Verify ECS Environment Variables

Executed:

```bash
aws ecs describe-task-definition \
  --task-definition investment-tracker-api \
  --query "taskDefinition.containerDefinitions[0].environment"
```

Result:

```json
[
  {
    "name": "ASPNETCORE_ENVIRONMENT",
    "value": "Production"
  },
  {
    "name": "Cors__AllowedOrigin",
    "value": "http://investment-tracker-frontend-648548511587.s3-website-us-east-1.amazonaws.com"
  }
]
```

CORS origin configured correctly.

---

## 7. Verify Running Task Definition

Executed:

```bash
aws ecs describe-services \
  --cluster investment-tracker-cluster \
  --services investment-tracker-api \
  --query "services[0].taskDefinition"
```

Result:

```text
arn:aws:ecs:us-east-1:648548511587:task-definition/investment-tracker-api:7
```

Confirmed service running task definition revision 7.

---

## 8. Verify CORS Preflight Request

Executed:

```bash
curl.exe -i -X OPTIONS \
"http://50.17.14.205:8080/api/offices/register" \
-H "Origin: http://investment-tracker-frontend-648548511587.s3-website-us-east-1.amazonaws.com" \
-H "Access-Control-Request-Method: POST"
```

Response:

```http
HTTP/1.1 204 No Content

Access-Control-Allow-Methods: POST

Access-Control-Allow-Origin:
http://investment-tracker-frontend-648548511587.s3-website-us-east-1.amazonaws.com
```

Result:

* CORS working correctly
* ECS configuration correct
* Browser should be allowed to call API

---

# Frontend Payload Analysis

Frontend app.js sends:

```json
{
  "officeName": "...",
  "managerName": "...",
  "email": "...",
  "phone": "...",
  "userName": "...",
  "password": "..."
}
```

Fetch implementation:

```javascript
const response = await fetch(API_URL, {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json'
    },
    body: JSON.stringify(payload)
});
```

No issue found in frontend code.

---

# Direct API Testing

Attempted direct POST request.

Response:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "$": [
      "'o' is an invalid start of a property name."
    ],
    "request": [
      "The request field is required."
    ]
  }
}
```

Important finding:

```text
"The request field is required."
```

This strongly suggests backend API expects a different JSON contract than frontend currently sends.

---

# Swagger Check

Attempted:

```bash
curl.exe -i http://50.17.14.205:8080/swagger/index.html
```

Response:

```http
404 Not Found
```

Swagger disabled in Production.

---

# Current Conclusion

Infrastructure appears healthy:

* S3 Frontend OK
* ECS Backend OK
* Health Endpoint OK
* CORS OK
* Network Connectivity OK

Most likely root cause:

Backend registration endpoint expects a different request model than the frontend sends.

Possible expected structure:

```json
{
  "request": {
    "officeName": "...",
    "managerName": "...",
    "email": "...",
    "phone": "...",
    "userName": "...",
    "password": "..."
  }
}
```

or another DTO/wrapper model.

---

# Required Next Step

Inspect backend source code and locate:

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register(...)
```

Also locate request model:

```csharp
RegisterOfficeRequest
```

or similar.

Verify:

1. Expected JSON schema
2. DTO structure
3. Controller action signature
4. Model binding attributes
5. Validation rules

Most likely fix will be either:

* Update frontend payload format
* Update backend DTO binding
* Remove unnecessary wrapper object
* Align frontend JSON with backend contract

```
```
