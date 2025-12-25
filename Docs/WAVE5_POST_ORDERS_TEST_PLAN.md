# Wave 5.1 - POST /api/v2/orders Test Plan

## Endpoint Specification
**URL:** POST /api/v2/orders  
**Authentication:** Required (Authorize header)  
**Status Code:** 201 Created (Success) | 400 Bad Request | 404 Not Found | 500 Internal Error

## Request Schema

```json
{
  "userId": "00000000-0000-0000-0000-000000000001",
  "requestId": null,
  "status": "pending",
  "items": [
    {
      "partId": "550e8400-e29b-41d4-a716-446655440001",
      "locationId": "550e8400-e29b-41d4-a716-446655440101",
      "quantity": 5,
      "unitPrice": 99.50
    }
  ]
}
```

## Response Schema (201 Created)

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440201",
  "requestId": null,
  "status": "pending",
  "totalAmount": 497.50,
  "createdAt": "2025-12-25T21:05:00Z",
  "shippedAt": null,
  "deliveredAt": null,
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440301",
      "partId": "550e8400-e29b-41d4-a716-446655440001",
      "partCode": "PART-001",
      "partName": "Example Part",
      "locationId": "550e8400-e29b-41d4-a716-446655440101",
      "locationName": "Warehouse A",
      "quantity": 5,
      "unitPrice": 99.50,
      "lineAmount": 497.50
    }
  ],
  "availabilityWarnings": []
}
```

## Test Cases

### TC1: Valid Order Creation (Happy Path)
**Given:**
- User ID exists in database
- All part IDs exist
- All location IDs exist
- Inventory available for requested quantities

**When:** POST /api/v2/orders with valid request

**Then:**
- Status Code: 201 Created
- Order ID returned in response
- Order status: "pending"
- All items included with correct calculations
- TotalAmount = sum of (Quantity * UnitPrice) for all items
- No availability warnings (or warnings if inventory low)

### TC2: Missing Request Body
**Given:** Empty request body

**When:** POST /api/v2/orders with null body

**Then:**
- Status Code: 400 Bad Request
- Error: "Request body and items are required"
- Error Code: "VALIDATION_ERROR"

### TC3: Missing Order Items
**Given:** Request with empty items array

**When:** POST /api/v2/orders with items: []

**Then:**
- Status Code: 400 Bad Request
- Error: "Request body and items are required"

### TC4: User Not Found
**Given:** Invalid userId (does not exist in database)

**When:** POST /api/v2/orders with unknown userId

**Then:**
- Status Code: 404 Not Found
- Error: "User not found: {userId}"
- Error Code: "NOT_FOUND"

### TC5: Part Not Found
**Given:** One part ID does not exist

**When:** POST /api/v2/orders with invalid partId

**Then:**
- Status Code: 404 Not Found
- Error: "Parts not found: {partId1}, {partId2}, ..."
- Error Code: "NOT_FOUND"

### TC6: Location Not Found
**Given:** Location ID provided but does not exist

**When:** POST /api/v2/orders with invalid locationId

**Then:**
- Status Code: 404 Not Found
- Error: "Locations not found: {locationId}"
- Error Code: "NOT_FOUND"

### TC7: Inventory Availability Warning
**Given:**
- All validation passes
- Part has fewer units than requested quantity

**When:** POST /api/v2/orders with quantity > QuantityOnHand

**Then:**
- Status Code: 201 Created (order still created)
- AvailabilityWarnings includes: "Part '{name}' has X units available but Y requested..."
- Order can still be created (reservation happens at approval stage)

### TC8: Multiple Items in Order
**Given:** Request with 3 order items

**When:** POST /api/v2/orders with multiple items

**Then:**
- Status Code: 201 Created
- Response includes all 3 items
- TotalAmount = sum of all line amounts
- Each item has correct LineAmount = Quantity * UnitPrice

### TC9: Order with Request Reference
**Given:** requestId provided

**When:** POST /api/v2/orders with requestId

**Then:**
- Status Code: 201 Created
- Order.RequestId = provided requestId
- Order history entry created with "created" event

### TC10: Calculation Accuracy
**Given:** Multiple items with decimal prices

**When:** POST /api/v2/orders with prices like 99.99, 199.99

**Then:**
- Each LineAmount calculated correctly: Quantity * UnitPrice
- TotalAmount = exact sum of all LineAmounts
- No floating-point rounding errors (use decimal type)

---

## Curl Test Examples

### Successful Order Creation
```bash
# Get auth token first
TOKEN="Bearer eyJhbGciOiJIUzI1NiIs..."

# Create order with 2 items
curl -X POST http://localhost:5000/api/v2/orders \
  -H "Authorization: $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "550e8400-e29b-41d4-a716-446655440001",
    "requestId": null,
    "status": "pending",
    "items": [
      {
        "partId": "550e8400-e29b-41d4-a716-446655440001",
        "locationId": "550e8400-e29b-41d4-a716-446655440101",
        "quantity": 5,
        "unitPrice": 99.50
      },
      {
        "partId": "550e8400-e29b-41d4-a716-446655440002",
        "quantity": 10,
        "unitPrice": 49.99
      }
    ]
  }'
```

### Test Invalid Part ID
```bash
curl -X POST http://localhost:5000/api/v2/orders \
  -H "Authorization: $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "550e8400-e29b-41d4-a716-446655440001",
    "items": [
      {
        "partId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "unitPrice": 100.00
      }
    ]
  }'

# Expected: 404 Not Found with "Parts not found: 00000000-0000-0000-0000-000000000000"
```

---

## Validation Checklist

### Code Review
- [ ] Request validation checks all required fields
- [ ] User existence verified before order creation
- [ ] All part IDs validated against Parts table
- [ ] All location IDs validated against Locations table
- [ ] TotalAmount calculated from items (not from request)
- [ ] Quantity validation (positive integers)
- [ ] UnitPrice validation (positive decimals)
- [ ] Order and OrderItems added to DbContext
- [ ] SaveChangesAsync called for persistence
- [ ] Order history entry created with correct event type
- [ ] Response includes all items with navigation properties (Part, Location names)
- [ ] Availability warnings collected for low-inventory parts

### Error Handling
- [ ] Empty/null request returns 400 with specific error
- [ ] Unknown user returns 404 "User not found"
- [ ] Unknown parts return 404 with specific part IDs
- [ ] Unknown locations return 404 with specific location IDs
- [ ] Database errors caught and return 500 with error message
- [ ] All error responses include error code and message
- [ ] No stack traces in production responses

### Integration Points
- [ ] CloudWatcherContext injected and available
- [ ] CurrentUserId extracted from authorization token
- [ ] Order ID generated as Guid.NewGuid()
- [ ] CreatedAt set to DateTime.UtcNow
- [ ] Status defaults to "pending" if not provided
- [ ] Line amounts calculated consistently
- [ ] Inventory checks non-blocking (warnings only)

---

## Build Verification

**Last Build Status:** ✅ SUCCESSFUL
- 0 Errors
- 50 Pre-existing Warnings (unchanged)
- Build Time: ~4 seconds

**Code Structure Verified:**
- OrdersControllerV2 class structure intact
- Authorize attribute on controller
- POST endpoint with [HttpPost] attribute
- CreateOrderRequest DTO structure complete
- OrderResponse mapping with AvailabilityWarnings
- Error handling with ErrorResponse class

---

## Status

**W5.1 Complete Summary:**
| Subtask | Status | Completion |
|---------|--------|-----------|
| W5.1.1 - Models | ✅ | Order/OrderItem models with all properties |
| W5.1.2 - POST Endpoint | ✅ | Endpoint with full validation (parts, locations, users) |
| W5.1.3 - Inventory Checks | ✅ | Availability warnings for low-stock parts |
| W5.1.4 - Test Plan | ✅ | 10 test cases + curl examples + validation checklist |

**All W5.1 subtasks complete. Ready for execution of W5.2 (GET /api/v2/orders).**

---

**Document Version:** 1.0  
**Created:** 2025-12-25  
**Status:** Ready for Testing
