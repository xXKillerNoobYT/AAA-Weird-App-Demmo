# Wave 4 Deployment Guide

**CloudWatcher API - Availability Calculation Enhancement**  
**Version:** 2.0  
**Release Date:** December 2025  
**Deployment Type:** Database + Code Deployment

## Overview

Wave 4 introduces enhanced availability calculations including reserved units, incoming purchase orders, backorder tracking, and effective available units. This guide covers deployment to development, staging, and production environments.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Pre-Deployment Checklist](#pre-deployment-checklist)
3. [Deployment Steps](#deployment-steps)
4. [Validation & Testing](#validation--testing)
5. [Rollback Procedure](#rollback-procedure)
6. [Post-Deployment](#post-deployment)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software

- **.NET 10.0 SDK** (minimum version 10.0.100)
  - Download: https://dot net.microsoft.com/download/dotnet/10.0
  - Verify: `dotnet --version` should show `10.0.x`

- **PostgreSQL 15+**
  - Connection string configured in `appsettings.{Environment}.json`
  - User must have CREATE TABLE permissions for migrations

- **Git** (for version control)
  - Version: 2.30+

### Environment Variables

**Development:**
```bash
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000
ConnectionStrings__CloudWatcherDb=Host=localhost;Port=5432;Database=CloudWatcherDev;Username=postgres;Password=devpassword
```

**Staging:**
```bash
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://staging-server:5000
ConnectionStrings__CloudWatcherDb=Host=staging-db;Port=5432;Database=CloudWatcherStaging;Username=cloudwatcher_app;Password=<secure-password>
```

**Production:**
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://api.cloudwatcher.com
ConnectionStrings__CloudWatcherDb=Host=prod-db;Port=5432;Database=CloudWatcherProd;Username=cloudwatcher_app;Password=<secure-password>
```

### Access Requirements

- **Database Access:** Credentials with DDL permissions (for migrations)
- **Server Access:** SSH/RDP to deployment server
- **Source Control:** Access to GitHub repository
- **Deployment Pipeline:** CI/CD credentials (if using automated deployment)

---

## Pre-Deployment Checklist

### 1. Code Review ✅
- [ ] All Wave 4 changes code-reviewed and approved
- [ ] No merge conflicts in main branch
- [ ] All unit tests passing locally
- [ ] Static analysis warnings addressed

### 2. Database Backup ✅
```bash
# PostgreSQL backup
pg_dump -h localhost -U postgres -d CloudWatcherDev > backup_wave4_pre_$(date +%Y%m%d_%H%M%S).sql

# Verify backup file created
ls -lh backup_wave4_pre_*.sql
```

**Storage:** Copy backup to secure location (S3, network share, etc.)

### 3. Dependencies Check ✅
```bash
# Verify .NET SDK
dotnet --version  # Should be 10.0.x

# Verify PostgreSQL client
psql --version  # Should be 15.x+

# Check NuGet package compatibility
cd server/CloudWatcher
dotnet restore
```

### 4. Environment Configuration ✅
- [ ] Database connection strings verified
- [ ] API keys configured (if applicable)
- [ ] CORS settings updated for new endpoints
- [ ] Logging level appropriate for environment

### 5. Maintenance Window ✅
- [ ] Maintenance window scheduled (estimate: 30 minutes)
- [ ] Users notified of downtime
- [ ] Rollback plan reviewed with team
- [ ] Stakeholders informed of deployment timeline

---

## Deployment Steps

### Step 1: Stop Current Application

**Development (localhost):**
```powershell
# Find CloudWatcher process
Get-Process -Name "CloudWatcher" -ErrorAction SilentlyContinue | Stop-Process -Force

# Or if running in terminal
# Press Ctrl+C in the terminal running the server
```

**Staging/Production (systemd):**
```bash
sudo systemctl stop cloudwatcher-api
sudo systemctl status cloudwatcher-api  # Verify stopped
```

**Staging/Production (IIS):**
```powershell
Stop-WebAppPool -Name "CloudWatcherAPI"
Stop-Website -Name "CloudWatcherAPI"
```

---

### Step 2: Pull Latest Code

```bash
cd /path/to/AAA\ Weird\ App\ Demmo

# Fetch latest changes
git fetch origin

# Checkout Wave 4 release branch/tag
git checkout wave4-release  # Or specific tag: v2.0.0

# Verify correct version
git log -1 --oneline
```

**Expected Output:**
```
abc1234 Wave 4: Availability calculation enhancements
```

---

### Step 3: Build Application

```bash
cd server/CloudWatcher

# Clean previous build
dotnet clean --configuration Release

# Restore dependencies
dotnet restore

# Build for production
dotnet build --configuration Release --no-restore

# Verify build succeeded
echo $?  # Should be 0 (Linux/Mac)
echo $LASTEXITCODE  # Should be 0 (PowerShell)
```

**Build Output:**
```
Build succeeded.
    X Warning(s)
    0 Error(s)
```

**Note:** Warnings related to nullable references are acceptable (non-blocking).

---

### Step 4: Run Database Migrations

**CRITICAL:** This step modifies the database schema. Ensure backup completed in pre-deployment checklist.

#### Option A: Automatic Migration (Recommended for Dev/Staging)

```bash
# Run migrations via dotnet ef
dotnet ef database update --project server/CloudWatcher

# Verify migration applied
dotnet ef migrations list --project server/CloudWatcher
```

**Expected Output:**
```
<timestamp>_InitialCreate (Applied)
<timestamp>_AddWave4AvailabilityFields (Applied) ✓
```

#### Option B: Manual SQL Script (Recommended for Production)

```bash
# Generate SQL script from migrations
dotnet ef migrations script --project server/CloudWatcher --output wave4_migration.sql

# Review SQL script
cat wave4_migration.sql

# Apply manually via psql
psql -h prod-db -U cloudwatcher_app -d CloudWatcherProd -f wave4_migration.sql

# Verify schema changes
psql -h prod-db -U cloudwatcher_app -d CloudWatcherProd -c "\d orders"
psql -h prod-db -U cloudwatcher_app -d CloudWatcherProd -c "\d purchase_orders"
```

**Note:** If no schema changes in Wave 4 (enhancement to existing tables only), this step may show "No pending migrations."

---

### Step 5: Seed Test Data (Development Only)

**⚠️ SKIP THIS STEP IN STAGING/PRODUCTION**

```bash
cd server/CloudWatcher

# Run with seeding argument
dotnet run --configuration Debug --seed-wave4

# Wait for confirmation message
# Expected: "✅ Wave 4 test data seeded successfully"

# Stop server after seeding (Ctrl+C)
```

**Verification:**
```sql
-- Check test data
psql -h localhost -U postgres -d CloudWatcherDev -c "
  SELECT * FROM parts WHERE code = 'PART-001';
  SELECT * FROM orders WHERE id IN (
    SELECT DISTINCT order_id FROM order_items WHERE part_id = '550e8400-e29b-41d4-a716-446655440000'
  );
"
```

---

### Step 6: Start Application

**Development (localhost):**
```powershell
cd server/CloudWatcher
dotnet run --configuration Release
```

**Staging/Production (systemd):**
```bash
sudo systemctl start cloudwatcher-api
sudo systemctl status cloudwatcher-api  # Verify running

# Check logs
sudo journalctl -u cloudwatcher-api -n 50 --no-pager
```

**Staging/Production (IIS):**
```powershell
Start-WebAppPool -Name "CloudWatcherAPI"
Start-Website -Name "CloudWatcherAPI"

# Verify app pool running
Get-WebAppPoolState -Name "CloudWatcherAPI"
```

**Expected Log Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

---

## Validation & Testing

### Test 1: Health Check

```bash
curl http://localhost:5000/health
```

**Expected Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-12-25T10:00:00Z"
}
```

---

### Test 2: Availability Endpoint

```bash
# Development
curl -X GET "http://localhost:5000/api/v2/inventory/550e8400-e29b-41d4-a716-446655440000/availability" \
  -H "X-Api-Key: dev-local-key"

# Production (with actual part ID)
curl -X GET "https://api.cloudwatcher.com/api/v2/inventory/{actual-part-id}/availability" \
  -H "Authorization: Bearer {jwt-token}"
```

**Expected Response Structure:**
```json
{
  "partId": "...",
  "partCode": "...",
  "partName": "...",
  "totalQuantityOnHand": 100,
  "totalReserved": 35,
  "totalAvailable": 65,
  "reservedUnits": 35,
  "incomingUnits": 50,
  "backorderedUnits": 0,
  "effectiveAvailableUnits": 115,
  "locationCount": 3,
  "locations": [...]
}
```

**Validate:**
- ✅ All new fields present: `reservedUnits`, `incomingUnits`, `backorderedUnits`, `effectiveAvailableUnits`
- ✅ Calculation correct: `effectiveAvailable = onHand - reserved + incoming - backorder`
- ✅ HTTP 200 status code

---

### Test 3: Run Automated Test Suite

```bash
cd server/CloudWatcher

# Development: Run PowerShell tests
.\Tests\TestWave4AllCases.ps1

# Expected: All 8 test cases PASS
```

**Expected Output:**
```
✅ Test 1: All factors calculation: PASS
✅ Test 2: Reserved units from DB: PASS
✅ Test 3: Incoming units from DB: PASS
✅ Test 4: Backorder calculation: PASS
✅ Test 5: Location availability: PASS
✅ Test 6: Reserved consistency: PASS
✅ Test 7: Effective available formula: PASS
✅ Test 8: Response structure: PASS

Total: 8 PASS, 0 FAIL, 0 WARNING, 0 ERROR
🎉 ALL TESTS PASSED!
```

---

### Test 4: Integration Test

**Scenario:** Complete order-to-availability workflow

```bash
# 1. Check initial availability
curl -X GET "{baseUrl}/api/v2/inventory/{partId}/availability"
# Note: effectiveAvailableUnits

# 2. Create new order (reserves units)
curl -X POST "{baseUrl}/api/v1/orders" \
  -H "Content-Type: application/json" \
  -d '{
    "items": [{"partId": "{partId}", "quantity": 10}]
  }'

# 3. Re-check availability (should show +10 reserved, -10 effectiveAvailable)
curl -X GET "{baseUrl}/api/v2/inventory/{partId}/availability"

# 4. Approve order
curl -X PATCH "{baseUrl}/api/v1/orders/{orderId}" \
  -H "Content-Type: application/json" \
  -d '{"status": "approved"}'

# 5. Final availability check (reserved units should persist)
curl -X GET "{baseUrl}/api/v2/inventory/{partId}/availability"
```

**Validation:**
- Reserved units increase by order quantity
- Effective available decreases by order quantity
- Backorder calculated correctly if order exceeds stock

---

### Test 5: Performance Test

```bash
# Apache Bench (if installed)
ab -n 100 -c 10 -H "X-Api-Key: dev-local-key" \
  "http://localhost:5000/api/v2/inventory/550e8400-e29b-41d4-a716-446655440000/availability"
```

**Expected:**
- 50th percentile: < 100ms
- 95th percentile: < 200ms
- 99th percentile: < 500ms
- 0% error rate

---

## Rollback Procedure

### When to Rollback

- Critical bug discovered in Wave 4 calculations
- Database migration failed
- Application won't start after deployment
- Performance degradation > 50%

### Rollback Steps

#### Step 1: Stop Application

```bash
# systemd
sudo systemctl stop cloudwatcher-api

# IIS
Stop-WebAppPool -Name "CloudWatcherAPI"
Stop-Website -Name "CloudWatcherAPI"
```

#### Step 2: Restore Database

```bash
# Drop Wave 4 tables/columns (if schema changes exist)
psql -h localhost -U postgres -d CloudWatcherDev -c "
  -- Rollback migration (example - adjust to actual migration)
  ALTER TABLE order_items DROP COLUMN IF EXISTS location_id;
"

# OR restore from backup
pg_restore -h localhost -U postgres -d CloudWatcherDev -c backup_wave4_pre_20251225.sql
```

#### Step 3: Rollback Code

```bash
cd /path/to/AAA\ Weird\ App\ Demmo

# Checkout previous stable version
git checkout wave3-release  # Or previous tag

# Rebuild
cd server/CloudWatcher
dotnet clean
dotnet restore
dotnet build --configuration Release
```

#### Step 4: Restart Application

```bash
# systemd
sudo systemctl start cloudwatcher-api
sudo systemctl status cloudwatcher-api

# IIS
Start-WebAppPool -Name "CloudWatcherAPI"
Start-Website -Name "CloudWatcherAPI"
```

#### Step 5: Verify Rollback

```bash
# Health check
curl http://localhost:5000/health

# Verify old endpoint still works
curl -X GET "{baseUrl}/api/v2/inventory/{partId}/availability"

# Should show Wave 3 response format (without new fields)
```

---

## Post-Deployment

### Monitoring

**Key Metrics to Monitor (First 24 Hours):**

1. **Availability Endpoint Performance**
   - Latency (p50, p95, p99)
   - Error rate (should be <1%)
   - Throughput (requests per minute)

2. **Database Performance**
   - Query execution time for reserved/incoming calculations
   - Connection pool usage
   - Long-running queries

3. **Application Health**
   - Memory usage (should stabilize within 30 minutes)
   - CPU usage (should remain < 70%)
   - Exception rate (should be <0.1%)

**Tools:**
- Application Insights / New Relic for APM
- PostgreSQL slow query log
- systemd journal logs

---

### Documentation Updates

- [ ] Update API documentation with new response fields
- [ ] Update Swagger/OpenAPI specs
- [ ] Notify consumers of API changes (if breaking)
- [ ] Update changelog with Wave 4 release notes

---

### Communication

**Internal:**
- Notify development team of successful deployment
- Update project tracker (mark Wave 4 as deployed)
- Schedule retrospective meeting

**External (if applicable):**
- Notify API consumers of new fields available
- Update developer portal documentation
- Announce new features in release notes

---

## Troubleshooting

### Issue: Build Fails with "Target framework not found"

**Error:**
```
error MSB3644: The reference assemblies for .NETCoreApp,Version=v10.0 were not found
```

**Solution:**
```bash
# Install .NET 10.0 SDK
# Windows:
winget install Microsoft.DotNet.SDK.10

# Linux:
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0

# Verify
dotnet --version
```

---

### Issue: Migration Fails - "relation already exists"

**Error:**
```
Npgsql.PostgresException: 42P07: relation "order_items" already exists
```

**Solution:**
```bash
# Check current migration status
dotnet ef migrations list

# If migration was partially applied, rollback
dotnet ef database update <PreviousMigrationName>

# Re-apply migration
dotnet ef database update
```

---

### Issue: Application Starts but Availability Endpoint Returns 500

**Symptoms:**
- Health check returns 200
- Availability endpoint returns 500 Internal Server Error

**Debugging:**
```bash
# Check application logs
sudo journalctl -u cloudwatcher-api -n 100 --no-pager | grep -i error

# Look for:
# - Database connection errors
# - Null reference exceptions in InventoryControllerV2
# - LINQ query errors in reserved/incoming calculations
```

**Common Causes:**
- Missing `LocationId` in OrderItems (seed data issue)
- Database schema mismatch (migration not applied)
- Connection string incorrect

---

### Issue: Reserved Units Always Show 0

**Symptoms:**
- `totalReserved = 0` even with pending orders
- `reservedUnits = 0` but orders exist

**Root Cause:** OrderItems missing `location_id` column

**Verification:**
```sql
SELECT * FROM order_items WHERE location_id IS NULL;
```

**Solution:**
```sql
-- Update OrderItems to have valid location_id
UPDATE order_items oi
SET location_id = (
  SELECT id FROM locations LIMIT 1
)
WHERE oi.location_id IS NULL;
```

---

### Issue: Calculation Incorrect

**Symptoms:**
- `effectiveAvailableUnits` doesn't match expected calculation
- Example: Shows 150 instead of 115

**Debugging:**
```bash
# Run diagnostic test
.\Tests\TestWave4Availability.ps1

# Check intermediate values:
# - totalQuantityOnHand
# - totalReserved
# - reservedUnits (should equal totalReserved)
# - incomingUnits
# - backorderedUnits
```

**Common Causes:**
- Reserved calculation grouping by wrong field
- Incoming calculation not filtering by `status = 'approved'`
- Backorder using wrong formula

**Fix:** Review [InventoryControllerV2.cs](c:\Users\weird\AAA Weird App Demmo\server\CloudWatcher\Controllers\InventoryControllerV2.cs#L590-L610) calculation logic

---

## Appendix

### A. Environment-Specific Configuration

**Development (`appsettings.Development.json`):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "CloudWatcherDb": "Host=localhost;Port=5432;Database=CloudWatcherDev;Username=postgres;Password=devpassword"
  }
}
```

**Production (`appsettings.Production.json`):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "ConnectionStrings": {
    "CloudWatcherDb": "Host=prod-db;Port=5432;Database=CloudWatcherProd;Username=cloudwatcher_app;Password=${DB_PASSWORD}"
  }
}
```

---

### B. Test Data Reference

**Test Part:** PART-001 (Test Widget Alpha)  
**Part GUID:** 550e8400-e29b-41d4-a716-446655440000

**Expected Values (After Seeding):**
- Total On Hand: 100 units (50 + 30 + 20 across 3 locations)
- Reserved: 35 units (15 pending + 20 approved)
- Incoming: 50 units (1 approved PO)
- Backorder: 0 units (100 > 35)
- Effective Available: 115 units (100 - 35 + 50)

---

### C. SQL Verification Queries

```sql
-- Verify part exists
SELECT * FROM parts WHERE code = 'PART-001';

-- Verify inventory
SELECT location_id, quantity_on_hand 
FROM inventory 
WHERE part_id = '550e8400-e29b-41d4-a716-446655440000';

-- Verify orders
SELECT o.id, o.status, oi.quantity, oi.location_id
FROM orders o
JOIN order_items oi ON o.id = oi.order_id
WHERE oi.part_id = '550e8400-e29b-41d4-a716-446655440000';

-- Verify purchase orders
SELECT po.id, po.status, poi.quantity_ordered, poi.quantity_received
FROM purchase_orders po
JOIN purchase_order_items poi ON po.id = poi.purchase_order_id
WHERE poi.part_id = '550e8400-e29b-41d4-a716-446655440000';
```

---

**Deployment Guide Version:** 1.0  
**Last Updated:** December 2025  
**Related Guides:**  
- [Wave4 Test Results](Wave4-Test-Results.md)
- [User Guide - Availability](User-Guide-Availability.md)
- [Admin Guide - Backorders](Admin-Guide-Backorders.md)

---

**Prepared By:** Smart Execute Agent  
**Approved By:** [Pending stakeholder review]  
**Deployment Date:** [TBD - schedule maintenance window]
