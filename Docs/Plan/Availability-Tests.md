# Availability Endpoint Tests (Local-Only, No Cloud Auth)

**Status:** ✅ COMPLETED  
**Date:** December 25, 2025

## Environment

- API: [http://localhost:5000](http://localhost:5000)
- DB: PostgreSQL (EF Core 10 + Npgsql 10)
- Auth: JWT Bearer present; user requirement: remove cloud auth and use local-only

## Pre-checks

- Health: PASSED (`/health`) – DB connected, server healthy
- Swagger: Accessible (`/swagger`) – endpoints listed

## Attempted Calls

- `GET /api/v2/inventory`: 401 Unauthorized (auth enforced)
- `GET /api/v2/inventory/{partId}/availability`: expected to be protected as well

## Proposed Local Auth Bypass for Testing

To satisfy “100% local” and enable endpoint testing without Azure AD:

- Add a development-only API key header, e.g., `X-Api-Key: dev-local-key`, configurable via `appsettings.Development.json`.
- Middleware checks header and injects a fake identity with `roles: ["admin"]` for local testing only.
- Keep existing JWT Bearer for production; dev bypass active only when `ASPNETCORE_ENVIRONMENT=Development`.

## Availability Enrichment Output Schema

Target response fields to document and test:

- `availableUnits`: int (current stock)
- `reservedUnits`: int (sum of open order items)
- `incomingUnits`: int (sum of approved PO items not received)
- `backorderedUnits`: int (estimated outstanding backorders)
- `effectiveAvailableUnits`: int (available - reserved - backordered + incoming)

## Test Matrix (initial draft)

- No orders, no POs: reserved=0, incoming=0, backordered=0, effective=available
- Open orders only: reserved>0 reduces effective
- Approved POs pending receipt only: incoming>0 increases effective
- Backorders only: backordered>0 reduces effective
- Overlap cases: reserved & incoming present; ensure no double-count
- Partial receipts: PO partially received; incoming reflects remainder

## Test Execution Results

**Execution Date:** December 25, 2025, 6:51 PM UTC  
**Test Script:** `server/CloudWatcher/Tests/TestWave4AllCases.ps1`  
**Authentication:** Dev API key bypass (`X-Api-Key: dev-local-key`)

### Overall Results

🎉 **ALL 8 TEST CASES PASSED**

- Test 1: All factors calculation - ✅ PASS
- Test 2: Reserved units from DB - ✅ PASS  
- Test 3: Incoming units from DB - ✅ PASS
- Test 4: Backorder calculation - ✅ PASS
- Test 5: Location-level availability - ✅ PASS
- Test 6: Reserved consistency - ✅ PASS
- Test 7: Effective available formula - ✅ PASS
- Test 8: Response structure validation - ✅ PASS

### Actual API Response

**Endpoint:** `GET /api/v2/inventory/550e8400-e29b-41d4-a716-446655440000/availability`

```json
{
  "partId": "550e8400-e29b-41d4-a716-446655440000",
  "partCode": "PART-001",
  "partName": "Test Widget Alpha",
  "totalQuantityOnHand": 100,
  "totalReserved": 35,
  "totalAvailable": 65,
  "reservedUnits": 35,
  "incomingUnits": 50,
  "backorderedUnits": 0,
  "effectiveAvailableUnits": 115,
  "locationCount": 3,
  "locations": [
    {
      "locationId": "661e8511-f30c-41d4-a716-557788990000",
      "locationName": "Unknown",
      "quantityOnHand": 50,
      "reservedQuantity": 15,
      "availableQuantity": 35
    },
    {
      "locationId": "883f0733-152e-63f6-c938-779900212222",
      "locationName": "Unknown",
      "quantityOnHand": 20,
      "reservedQuantity": 0,
      "availableQuantity": 20
    },
    {
      "locationId": "772f9622-041d-52e5-b827-668899101111",
      "locationName": "Unknown",
      "quantityOnHand": 30,
      "reservedQuantity": 20,
      "availableQuantity": 10
    }
  ]
}
```

### Field Validation

| Field | Expected | Actual | Status |
|-------|----------|--------|--------|
| totalQuantityOnHand | 100 | 100 | ✅ |
| reservedUnits | 35 | 35 | ✅ |
| incomingUnits | 50 | 50 | ✅ |
| backorderedUnits | 0 | 0 | ✅ |
| effectiveAvailableUnits | 115 | 115 | ✅ |

**Formula Verification:**
- totalAvailable = onHand - reserved = 100 - 35 = 65 ✅
- backorder = max(0, reserved - onHand) = max(0, 35 - 100) = 0 ✅
- effective = totalAvailable - backorder + incoming = 65 - 0 + 50 = 115 ✅

### Known Issues

⚠️ **Minor:** Location names show as "Unknown" instead of actual names ("Test Warehouse", "Test Retail Store", "Test Delivery Truck"). This is due to missing eager loading of the Location navigation property. Tracked in Task D2.

## Next Steps

1. ~~Implement dev-only API key bypass for local testing (no cloud)~~ ✅ COMPLETED
2. ~~Execute availability endpoint tests~~ ✅ COMPLETED
3. ~~Document test results~~ ✅ COMPLETED
4. Fix location names showing as "Unknown" (Task D2) - Optional
5. Add xUnit integration tests for regression prevention (Task D5)

## Test Results (December 25, 2025)

### ✅ Dev API Key Bypass Implemented

**Configuration**:

- File: `server/CloudWatcher/appsettings.Development.json`
- Setting: `LocalAuth:ApiKey = "dev-local-key"`
- Middleware: `LocalDevApiKeyMiddleware.cs`
- Pipeline position: BEFORE `UseAuthentication()` to inject identity early

**Identity Injected**:

- Name: `dev-local`
- Email: `dev@local`
- Role: `admin`

### ✅ Inventory List Endpoint Test

**Request**:

```powershell
$headers = @{ "X-Api-Key" = "dev-local-key" }
Invoke-RestMethod -Uri "http://localhost:5000/api/v2/inventory" -Headers $headers
```

**Response**: 200 OK

```json
{
  "totalCount": 0,
  "items": []
}
```

**Status**: PASSED (database is empty, which is expected)

### ✅ Availability Endpoint Test

**Request**:

```powershell
$headers = @{ "X-Api-Key" = "dev-local-key" }
$testPartId = "550e8400-e29b-41d4-a716-446655440000"
Invoke-RestMethod -Uri "http://localhost:5000/api/v2/inventory/$testPartId/availability" -Headers $headers
```

**Response**: 404 Not Found (expected - part doesn't exist in empty database)

**Logs**:

```text
info: CloudWatcher.Controllers.InventoryControllerV2[0]
      Getting availability for partId=550e8400-e29b-41d4-a716-446655440000
warn: CloudWatcher.Controllers.InventoryControllerV2[0]
      Part not found: 550e8400-e29b-41d4-a716-446655440000
```

**Status**: PASSED (endpoint accessible, proper 404 response)

## Bug Fixes Applied

### Fixed EF Core Include Error

**Issue**: `InventoryControllerV2.cs` line 112 had `.Include(p => p.Code)` which is invalid since `Code` is a property, not a navigation.

**Fix**: Changed to `.Include(p => p.InventoryRecords)` to load the correct navigation.

**Files Modified**:

- `server/CloudWatcher/Controllers/InventoryControllerV2.cs`

### Fixed Middleware Ordering

**Issue**: Middleware was positioned AFTER `UseAuthentication()`, causing JWT authentication to challenge before dev identity could be injected.

**Fix**: Moved `LocalDevApiKeyMiddleware` to run BEFORE `UseAuthentication()`.

**Files Modified**:

- `server/CloudWatcher/Program.cs`

## Readiness Assessment

✅ **Authentication bypass working**: Local testing enabled without cloud auth

✅ **Endpoints accessible**: Inventory list and availability endpoints respond correctly

⚠️ **Database empty**: Need sample data to test enriched availability calculations

## Next Phase: Data Seeding & Enrichment Testing

1. Populate small sample data set for Inventory, OrderItems, PurchaseOrderItems
2. Call `GET /api/v2/inventory/{partId}/availability` and capture outputs
3. Document results and edge cases in this file
4. Implement schema extensions (reservedUnits, incomingUnits, backorderedUnits, effectiveAvailableUnits)
5. Run acceptance test matrix
6. Review indexing strategy

## Notes

- Dev auth bypass implemented and verified ✅
- Sample data seeding pending for enrichment testing

