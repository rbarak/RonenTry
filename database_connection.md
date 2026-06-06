# Database Connection Guide

How to connect to the Investment Tracker RDS SQL Server from VS Code and run queries.

---

## Prerequisites

- VS Code installed
- AWS CLI configured (`C:\Program Files\Amazon\AWSCLIV2\aws.exe`)

---

## Step 1 — Install the SQL Server Extension

1. Open VS Code
2. Click the **Extensions** icon in the left sidebar (or press `Ctrl+Shift+X`)
3. Search for **SQL Server (mssql)**
4. Install the extension by **Microsoft**
5. Reload VS Code if prompted

---

## Step 2 — Open Port 1433 on RDS

RDS is in a **private subnet** — port 1433 is blocked by default. You must temporarily open it from your IP before connecting.

```powershell
$AWS = "C:\Program Files\Amazon\AWSCLIV2\aws.exe"

# Get your current public IPv4 address
$MY_IP = curl.exe -s -4 https://checkip.amazonaws.com
$MY_IP = $MY_IP.Trim()
Write-Host "Your IP: $MY_IP"

# Open port 1433 for your IP only
& $AWS ec2 authorize-security-group-ingress `
  --group-id sg-00d6a98ed349a1079 `
  --protocol tcp --port 1433 `
  --cidr "$MY_IP/32" `
  --region us-east-1
```

---

## Step 3 — Connect in VS Code

1. Click the **SQL Server** icon in the left sidebar (database cylinder icon)
2. Click **Add Connection** (the `+` button)
3. Fill in the connection details:

| Field | Value |
|---|---|
| **Server** | `investment-tracker-db.cc9isgm6qkub.us-east-1.rds.amazonaws.com,1433` |
| **Authentication Type** | `SQL Login` |
| **User name** | `sa` |
| **Password** | `InvestSql969!aB9` |
| **Remember password** | Yes |
| **Database** | `InvestmentTracker` |
| **Encrypt** | `True` |
| **Trust Server Certificate** | `True` |
| **Connection Name** | `Investment Tracker RDS` |

4. Click **Connect**
5. The database tree appears in the sidebar: `InvestmentTracker` → `Tables` → `dbo.Offices`

---

## Step 4 — Run Queries

1. Right-click on **`InvestmentTracker`** database in the sidebar
2. Click **New Query**
3. A query editor tab opens — paste any query below
4. Press **`Ctrl+Shift+E`** to execute (or click the **Run** button)
5. Results appear in the panel below

---

## Useful Queries

```sql
-- View all registered offices (newest first)
SELECT Id, OfficeId, OfficeName, ManagerName, Email, Phone, UserName, RegistrationDate
FROM Offices
ORDER BY RegistrationDate DESC;

-- Count total registrations
SELECT COUNT(*) AS TotalOffices FROM Offices;

-- Find a specific office by phone
SELECT * FROM Offices WHERE Phone = '0521234567';

-- Find by office ID
SELECT * FROM Offices WHERE OfficeId = 111;

-- Check which EF Core migrations have been applied
SELECT * FROM __EFMigrationsHistory;

-- View table schema
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Offices'
ORDER BY ORDINAL_POSITION;

-- Check the OfficeId sequence current value
SELECT current_value FROM sys.sequences WHERE name = 'OfficeIdSequence';
```

---

## Step 5 — Close Port When Done

**Always close the port after you finish** — leaving 1433 open exposes the database to the internet.

```powershell
$AWS = "C:\Program Files\Amazon\AWSCLIV2\aws.exe"
$MY_IP = curl.exe -s -4 https://checkip.amazonaws.com
$MY_IP = $MY_IP.Trim()

& $AWS ec2 revoke-security-group-ingress `
  --group-id sg-00d6a98ed349a1079 `
  --protocol tcp --port 1433 `
  --cidr "$MY_IP/32" `
  --region us-east-1

Write-Host "Port 1433 closed for $MY_IP"
```

---

## Connection Details Reference

| Item | Value |
|---|---|
| RDS Endpoint | `investment-tracker-db.cc9isgm6qkub.us-east-1.rds.amazonaws.com` |
| Port | `1433` |
| Database | `InvestmentTracker` |
| Username | `sa` |
| Password | stored in Secrets Manager (see below) |
| RDS Security Group | `sg-00d6a98ed349a1079` |
| Region | `us-east-1` |

**Retrieve credentials from Secrets Manager:**
```powershell
$AWS = "C:\Program Files\Amazon\AWSCLIV2\aws.exe"
& $AWS secretsmanager get-secret-value `
  --secret-id "arn:aws:secretsmanager:us-east-1:648548511587:secret:investment-tracker/db-connection-KMHXlO" `
  --query 'SecretString' --output text --region us-east-1
```

---

## Manual Schema Recreation

If the database needs to be rebuilt from scratch (e.g. new RDS instance), use the idempotent SQL script:

```bash
# From repo root — run against RDS
sqlcmd -S investment-tracker-db.cc9isgm6qkub.us-east-1.rds.amazonaws.com,1433 \
  -U sa -P "InvestSql969!aB9" \
  -i infrastructure/schema.sql
```

The script creates the sequence, table, indexes, and writes the `__EFMigrationsHistory` entry so EF Core won't re-apply the migration on next startup.
