# User Guide: Part Availability API

## Overview

The CloudWatcher Availability API provides real-time visibility into part availability across all warehouse locations, taking into account current inventory, reserved units from orders, and incoming shipments from purchase orders.

**Endpoint:** `GET /api/v2/inventory/{partId}/availability`

## Quick Start

### Basic Request

```bash
curl -X GET "http://localhost:5000/api/v2/inventory/{partId}/availability" \
  -H "X-Api-Key: dev-local-key"
```

### Example Response

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
  "checkedAt": "2025-12-25T09:55:28.2857998Z",
  "locations": [
    {
      "locationId": "661e8511-f30c-41d4-a716-557788990000",
      "locationName": "Warehouse",
      "quantityOnHand": 50,
      "reservedQuantity": 15,
      "availableQuantity": 35,
      "reorderLevel": 20,
      "isLowStock": false,
      "needsReorder": false
    },
    {
      "locationId": "772f9622-041d-52e5-b827-668899101111",
      "locationName": "Retail",
      "quantityOnHand": 30,
      "reservedQuantity": 20,
      "availableQuantity": 10,
      "reorderLevel": 10,
      "isLowStock": false,
      "needsReorder": false
    },
    {
      "locationId": "883f0733-152e-63f6-c938-779900212222",
      "locationName": "Delivery Truck",
      "quantityOnHand": 20,
      "reservedQuantity": 0,
      "availableQuantity": 20,
      "reorderLevel": 5,
      "isLowStock": false,
      "needsReorder": false
    }
  ]
}
```

## Understanding the Response

### Key Availability Metrics

#### 1. **totalQuantityOnHand**
Physical inventory currently in stock across all locations.

**Example:** `100` - You have 100 units physically in your warehouses

**Use Case:** Basic inventory count, stocktaking verification

---

#### 2. **totalReserved**
Sum of reserved quantities per location (units allocated to pending or approved orders).

**Example:** `35` - 35 units are reserved for existing orders

**Use Case:** Understanding how much inventory is already committed

---

#### 3. **totalAvailable**
Units physically available for new orders (not yet reserved).

**Formula:** `totalAvailable = totalQuantityOnHand - totalReserved`

**Example:** `100 - 35 = 65` - Only 65 units are available for new orders right now

**Use Case:** Checking if you can fulfill a new order immediately

---

#### 4. **reservedUnits**
Total units reserved from pending and approved orders (same as totalReserved in current implementation).

**Example:** `35` - Reserved for customers

**Use Case:** Analyzing order fulfillment commitments

---

#### 5. **incomingUnits**
Units on order from suppliers (approved purchase orders not yet received).

**Formula:** `SUM(QuantityOrdered - QuantityReceived)` for approved POs

**Example:** `50` - Expecting 50 more units from supplier

**Use Case:** Planning for future availability, restocking timeline

---

#### 6. **backorderedUnits**
Units that customers ordered but you don't have in stock (when demand exceeds supply).

**Formula:** `MAX(0, reservedUnits - totalQuantityOnHand)`

**Example:** `0` - No backorders (supply exceeds demand)

**Example (backorder scenario):**  
If you had 100 reserved but only 80 on hand: `MAX(0, 100 - 80) = 20 backorders`

**Use Case:** Identifying shortage situations, prioritizing incoming shipments

---

#### 7. **effectiveAvailableUnits** ⭐ **MOST IMPORTANT**
The total units you'll have available once incoming shipments arrive.

**Formula:** `effectiveAvailable = totalAvailable + incomingUnits - backorderedUnits`

Or equivalently: `effectiveAvailable = totalQuantityOnHand - reservedUnits + incomingUnits - backorderedUnits`

**Example:** `65 + 50 - 0 = 115` - After receiving your pending shipment, you'll have 115 units available

**Use Case:** 
- **Planning future orders:** "Can I accept a 100-unit order next week?"
- **Sales forecasting:** "How much inventory will I have after restocking?"
- **Capacity planning:** "Should I place another purchase order?"

---

### Location-Level Details

Each location in the `locations` array provides granular availability:

```json
{
  "locationId": "661e8511-f30c-41d4-a716-557788990000",
  "locationName": "Warehouse",
  "quantityOnHand": 50,        // Physical inventory at this location
  "reservedQuantity": 15,       // Units reserved for orders at this location
  "availableQuantity": 35,      // Available for new orders: 50 - 15 = 35
  "reorderLevel": 20,           // Reorder threshold
  "isLowStock": false,          // Is current stock below reorder level?
  "needsReorder": false         // Is available stock below reorder level?
}
```

**Use Cases:**
- **Warehouse Selection:** Pick from location with highest `availableQuantity`
- **Reorder Alerts:** Monitor `needsReorder` flag to trigger purchase orders
- **Stock Balancing:** Transfer inventory from high-stock to low-stock locations

## Common Use Cases

### Use Case 1: Can I Fulfill This Order?

**Question:** Customer wants to order 80 units. Can I fulfill it?

**Check:** `totalAvailable >= 80`

**Example:**
- totalAvailable = 65 → ❌ **NO** (only 65 available immediately)
- totalAvailable = 85 → ✅ **YES** (85 available, can fulfill 80)

**If NO, when can I fulfill it?**
- Check `effectiveAvailableUnits` = 115
- 115 >= 80 → ✅ **YES, after incoming shipment arrives**

---

### Use Case 2: Should I Place a Purchase Order?

**Question:** Am I running low on stock?

**Check Multiple Indicators:**

1. **Immediate Shortage:** `totalAvailable < reorderLevel`
   - Example: Available = 65, Reorder Level = 70 → ⚠️ Low stock!

2. **Future Shortage:** `effectiveAvailableUnits < safetyStockLevel`
   - Example: Effective = 115, Safety Stock = 150 → ⚠️ Order more!

3. **Location-Specific:** Check `needsReorder` flag per location
   - If any location has `needsReorder = true` → ⚠️ Reorder needed

**Decision:**
- ✅ Place PO if any indicator shows low stock
- 📊 Calculate order quantity to reach safety stock level

---

### Use Case 3: Understanding Backorders

**Scenario:** More orders than inventory

**Example Data:**
```json
{
  "totalQuantityOnHand": 80,
  "reservedUnits": 100,
  "backorderedUnits": 20,
  "incomingUnits": 50,
  "effectiveAvailableUnits": 30
}
```

**Interpretation:**
- 📦 **On Hand:** 80 units physically in stock
- 📋 **Reserved:** 100 units promised to customers
- ⚠️ **Backordered:** 20 units short (100 - 80 = 20)
- 🚚 **Incoming:** 50 units on the way from supplier
- ✅ **After Shipment:** 30 units will be available (80 - 100 + 50 = 30)

**Action Items:**
1. Communicate backorder situation to customers (20 units delayed)
2. Prioritize incoming shipment to fulfill backordered units first
3. After receiving 50 units, fulfill the 20 backorders, leaving 30 available

---

### Use Case 4: Multi-Location Fulfillment

**Scenario:** Order for 60 units - which location should ship it?

**Strategy:** Prefer locations with highest available quantity

**Example Locations:**
1. Warehouse: 35 available
2. Retail: 10 available
3. Delivery Truck: 20 available

**Decision:**
- **Option A:** Ship 35 from Warehouse + 25 from Delivery Truck (uses 2 locations)
- **Option B:** Ship 35 from Warehouse + 10 from Retail + 15 from Truck (uses 3 locations)
- **Optimal:** Option A (fewer shipments, lower cost)

**Implementation:**
```javascript
// Sort locations by available quantity (descending)
const sorted = response.locations.sort((a, b) => 
  b.availableQuantity - a.availableQuantity
);

// Allocate order quantity
let remaining = 60;
const shipments = [];

for (const loc of sorted) {
  if (remaining <= 0) break;
  
  const shipQty = Math.min(remaining, loc.availableQuantity);
  if (shipQty > 0) {
    shipments.push({ location: loc.locationName, quantity: shipQty });
    remaining -= shipQty;
  }
}
```

---

## API Integration Examples

### JavaScript / Node.js

```javascript
const axios = require('axios');

async function checkAvailability(partId) {
  try {
    const response = await axios.get(
      `http://localhost:5000/api/v2/inventory/${partId}/availability`,
      { headers: { 'X-Api-Key': 'dev-local-key' } }
    );
    
    const data = response.data;
    
    console.log(`Part: ${data.partCode} - ${data.partName}`);
    console.log(`Available Now: ${data.totalAvailable} units`);
    console.log(`After Restocking: ${data.effectiveAvailableUnits} units`);
    
    if (data.backorderedUnits > 0) {
      console.log(`⚠️ BACKORDER: ${data.backorderedUnits} units short`);
    }
    
    return data;
  } catch (error) {
    console.error('Error checking availability:', error.message);
    throw error;
  }
}

// Usage
checkAvailability('550e8400-e29b-41d4-a716-446655440000');
```

### Python

```python
import requests

def check_availability(part_id):
    url = f"http://localhost:5000/api/v2/inventory/{part_id}/availability"
    headers = {"X-Api-Key": "dev-local-key"}
    
    response = requests.get(url, headers=headers)
    response.raise_for_status()
    
    data = response.json()
    
    print(f"Part: {data['partCode']} - {data['partName']}")
    print(f"Available Now: {data['totalAvailable']} units")
    print(f"After Restocking: {data['effectiveAvailableUnits']} units")
    
    if data['backorderedUnits'] > 0:
        print(f"⚠️ BACKORDER: {data['backorderedUnits']} units short")
    
    return data

# Usage
check_availability('550e8400-e29b-41d4-a716-446655440000')
```

### PowerShell

```powershell
function Get-PartAvailability {
    param(
        [string]$PartId
    )
    
    $headers = @{ 'X-Api-Key' = 'dev-local-key' }
    $url = "http://localhost:5000/api/v2/inventory/$PartId/availability"
    
    $response = Invoke-RestMethod -Uri $url -Headers $headers
    
    Write-Host "Part: $($response.partCode) - $($response.partName)"
    Write-Host "Available Now: $($response.totalAvailable) units"
    Write-Host "After Restocking: $($response.effectiveAvailableUnits) units"
    
    if ($response.backorderedUnits -gt 0) {
        Write-Host "⚠️ BACKORDER: $($response.backorderedUnits) units short" -ForegroundColor Yellow
    }
    
    return $response
}

# Usage
Get-PartAvailability -PartId "550e8400-e29b-41d4-a716-446655440000"
```

---

## Error Handling

### Invalid Part ID
**HTTP 400 Bad Request**
```json
{
  "error": "partId must be a valid UUID"
}
```

**Solution:** Ensure partId is a valid UUID format (e.g., `550e8400-e29b-41d4-a716-446655440000`)

---

### Part Not Found
**HTTP 404 Not Found**
```json
{
  "error": "Part not found"
}
```

**Solution:** Verify the part exists in the database. Check `partId` spelling.

---

### Authentication Error
**HTTP 401 Unauthorized**

**Solution:** Include `X-Api-Key` header in development environment

---

## Best Practices

### 1. **Cache Availability Data Carefully**
Availability changes frequently (orders placed, shipments received). Cache for short periods only (e.g., 1-5 minutes).

### 2. **Use effectiveAvailableUnits for Planning**
Don't rely solely on `totalAvailable` - consider future incoming shipments when planning.

### 3. **Monitor Backorders Closely**
Set up alerts when `backorderedUnits > 0` - this indicates customer dissatisfaction risk.

### 4. **Location-Aware Fulfillment**
Use `locations` array to optimize shipping costs and delivery times.

### 5. **Reorder Automation**
Automatically trigger purchase orders when `needsReorder = true` for any location.

---

## Frequently Asked Questions

**Q: What's the difference between `totalAvailable` and `effectiveAvailableUnits`?**  
A: `totalAvailable` is what you can fulfill RIGHT NOW. `effectiveAvailableUnits` is what you'll have AFTER incoming shipments arrive. Use the latter for planning future orders.

**Q: Why does `totalReserved` differ from `reservedUnits`?**  
A: In the current implementation, they should match. `totalReserved` is the sum of per-location reserved quantities. `reservedUnits` is used in backorder calculations. If they differ, contact support.

**Q: Can I check availability for multiple parts at once?**  
A: Not currently. Make separate API calls per part. (Bulk endpoint planned for future release)

**Q: How often is availability data updated?**  
A: Real-time. Every API call queries the database for current inventory, orders, and purchase orders.

**Q: What does `needsReorder` mean?**  
A: It's `true` when `(quantityOnHand - reservedQuantity) < reorderLevel`. This means available inventory (after reservations) is below the reorder threshold.

---

## Changelog

**Version 2.0 (Wave 4)** - December 2025
- Added `reservedUnits` field
- Added `incomingUnits` field
- Added `backorderedUnits` field
- Added `effectiveAvailableUnits` field
- Enhanced location details with `needsReorder` flag

**Version 1.0** - Initial Release
- Basic availability calculation
- Location-level breakdown

---

**Need Help?** Contact the CloudWatcher API team or consult the Admin Guide for backend configuration details.
