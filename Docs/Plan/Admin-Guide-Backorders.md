# Admin Guide: Backorder Management

**CloudWatcher API - Wave 4 Enhancement**  
**For:** System Administrators, Operations Managers, Inventory Controllers  
**Version:** 2.0 (December 2025)

## Overview

Backorders occur when customer demand exceeds available inventory. This guide explains how the CloudWatcher system calculates, tracks, and helps you manage backorder situations to maintain customer satisfaction and optimize inventory levels.

## Table of Contents

1. [Understanding Backorders](#understanding-backorders)
2. [Backorder Calculation Logic](#backorder-calculation-logic)
3. [Monitoring Backorders](#monitoring-backorders)
4. [Backorder Resolution Strategies](#backorder-resolution-strategies)
5. [Database Queries](#database-queries)
6. [Automation & Alerts](#automation--alerts)
7. [Best Practices](#best-practices)
8. [Troubleshooting](#troubleshooting)

---

## Understanding Backorders

### What is a Backorder?

A **backorder** represents units that customers have ordered (and you've approved) but cannot currently fulfill due to insufficient inventory.

**Example Scenario:**
- Customer orders 100 widgets
- You approve the order (status = 'approved')
- Current inventory: only 80 widgets on hand
- **Result:** 20 widgets are backordered

### Backorder Formula

```
backorderedUnits = MAX(0, reservedUnits - totalQuantityOnHand)
```

**Where:**
- `reservedUnits` = Total quantity from orders with status 'pending' or 'approved'
- `totalQuantityOnHand` = Physical inventory across all locations

**Key Insight:** Backorders only occur when you've promised more units than you physically have.

### Why Backorders Happen

1. **Demand Spike:** Unexpected surge in customer orders
2. **Supply Delays:** Supplier shipment delayed or incomplete
3. **Inventory Shrinkage:** Damaged goods, theft, or counting errors
4. **Order Approval:** Approving orders before verifying stock availability
5. **Multi-Location Issues:** Inventory exists but in wrong location

---

## Backorder Calculation Logic

### Step-by-Step Calculation

#### Step 1: Calculate Reserved Units
Query all order items for orders with status 'pending' or 'approved':

```sql
SELECT SUM(oi.quantity) as reserved_units
FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE oi.part_id = '{partId}'
  AND o.status IN ('pending', 'approved');
```

**Example Result:** 100 units reserved

#### Step 2: Calculate On-Hand Inventory
Query all inventory records for the part:

```sql
SELECT SUM(i.quantity_on_hand) as total_on_hand
FROM inventory i
WHERE i.part_id = '{partId}';
```

**Example Result:** 80 units on hand

#### Step 3: Calculate Backorder
```
backorderedUnits = MAX(0, 100 - 80) = 20 units
```

#### Step 4: Calculate Effective Availability
Taking into account incoming shipments:

```
effectiveAvailable = totalOnHand - reservedUnits + incomingUnits - backorderedUnits
effectiveAvailable = 80 - 100 + 50 - 20 = 10 units
```

**Interpretation:** After incoming shipment of 50 units arrives, you'll have 10 units available after fulfilling the backorder.

---

## Monitoring Backorders

### Real-Time Availability Check

**API Endpoint:** `GET /api/v2/inventory/{partId}/availability`

**Check for Backorders:**
```bash
curl -X GET "http://localhost:5000/api/v2/inventory/{partId}/availability" \
  -H "X-Api-Key: dev-local-key"
```

**Response Indicators:**
```json
{
  "totalQuantityOnHand": 80,
  "reservedUnits": 100,
  "backorderedUnits": 20,
  "effectiveAvailableUnits": 10
}
```

**Alert Criteria:**
- ⚠️ `backorderedUnits > 0` → Immediate action required
- ⚠️ `effectiveAvailableUnits < 0` → Backorder persists even after incoming shipments

---

### Dashboard Queries

#### Query 1: List All Parts with Backorders

```sql
WITH reserved AS (
  SELECT 
    oi.part_id,
    SUM(oi.quantity) as reserved_qty
  FROM order_items oi
  JOIN orders o ON oi.order_id = o.id
  WHERE o.status IN ('pending', 'approved')
  GROUP BY oi.part_id
),
on_hand AS (
  SELECT 
    part_id,
    SUM(quantity_on_hand) as on_hand_qty
  FROM inventory
  GROUP BY part_id
)
SELECT 
  p.code as part_code,
  p.name as part_name,
  COALESCE(oh.on_hand_qty, 0) as on_hand,
  COALESCE(r.reserved_qty, 0) as reserved,
  GREATEST(0, COALESCE(r.reserved_qty, 0) - COALESCE(oh.on_hand_qty, 0)) as backorder_qty
FROM parts p
LEFT JOIN on_hand oh ON p.id = oh.part_id
LEFT JOIN reserved r ON p.id = r.part_id
WHERE COALESCE(r.reserved_qty, 0) > COALESCE(oh.on_hand_qty, 0)
ORDER BY backorder_qty DESC;
```

**Output Example:**
| part_code | part_name | on_hand | reserved | backorder_qty |
|-----------|-----------|---------|----------|---------------|
| WIDGET-A | Widget Alpha | 80 | 100 | 20 |
| GADGET-B | Gadget Beta | 50 | 75 | 25 |

---

#### Query 2: Backorder Summary by Customer

```sql
WITH inventory_summary AS (
  SELECT 
    part_id,
    SUM(quantity_on_hand) as total_on_hand
  FROM inventory
  GROUP BY part_id
)
SELECT 
  o.id as order_id,
  o.request_id as customer_request,
  o.status,
  oi.part_id,
  p.code as part_code,
  oi.quantity as ordered_qty,
  COALESCE(inv.total_on_hand, 0) as available_qty,
  GREATEST(0, oi.quantity - COALESCE(inv.total_on_hand, 0)) as backorder_qty,
  o.created_at as order_date
FROM orders o
JOIN order_items oi ON o.id = oi.order_id
JOIN parts p ON oi.part_id = p.id
LEFT JOIN inventory_summary inv ON oi.part_id = inv.part_id
WHERE o.status IN ('pending', 'approved')
  AND oi.quantity > COALESCE(inv.total_on_hand, 0)
ORDER BY o.created_at DESC;
```

**Use Case:** Identify which customers are affected by backorders and prioritize communication.

---

## Backorder Resolution Strategies

### Strategy 1: Expedite Incoming Shipments

**When:** Backorder exists but incoming PO will cover it

**Check:**
```sql
SELECT 
  SUM(poi.quantity_ordered - poi.quantity_received) as incoming
FROM purchase_order_items poi
JOIN purchase_orders po ON poi.purchase_order_id = po.id
WHERE poi.part_id = '{partId}'
  AND po.status = 'approved'
  AND NOT po.is_fully_received;
```

**Action:**
1. Contact supplier to expedite shipment
2. Request partial shipment if full order delayed
3. Consider air freight for critical backorders
4. Update customers with new ETA

**API Check:**
```json
{
  "backorderedUnits": 20,
  "incomingUnits": 50,
  "effectiveAvailableUnits": 30
}
```
✅ Incoming shipment (50) covers backorder (20) with surplus (30)

---

### Strategy 2: Emergency Purchase Order

**When:** No incoming shipments or insufficient incoming quantity

**Check:**
```json
{
  "backorderedUnits": 20,
  "incomingUnits": 0,
  "effectiveAvailableUnits": -20
}
```
❌ No incoming units - need emergency PO

**Action:**
1. Create expedited purchase order
2. Mark as `status = 'urgent'` for priority processing
3. Consider paying premium for rush delivery
4. Set `expected_delivery_date` based on supplier commitment

**SQL:**
```sql
-- Create emergency PO
INSERT INTO purchase_orders (id, supplier_id, status, order_date, expected_delivery_date)
VALUES (uuid_generate_v4(), '{supplierId}', 'urgent', NOW(), NOW() + INTERVAL '3 days');

-- Add PO item
INSERT INTO purchase_order_items (id, purchase_order_id, part_id, quantity_ordered, unit_price)
VALUES (uuid_generate_v4(), '{poId}', '{partId}', 30, {price});
```

---

### Strategy 3: Stock Transfer

**When:** Backorder at one location but inventory available at another

**Check Location Breakdown:**
```json
{
  "locations": [
    { "locationName": "Warehouse A", "availableQuantity": 0 },
    { "locationName": "Warehouse B", "availableQuantity": 50 }
  ]
}
```

**Action:**
1. Identify source location with excess inventory
2. Create transfer order to destination location
3. Update inventory records after transfer
4. Fulfill backorder from destination location

**SQL:**
```sql
-- Check if other locations have stock
SELECT 
  location_id,
  quantity_on_hand,
  quantity_on_hand - COALESCE(reserved_qty, 0) as available
FROM inventory i
LEFT JOIN (
  SELECT location_id, SUM(quantity) as reserved_qty
  FROM order_items
  WHERE part_id = '{partId}'
  GROUP BY location_id
) r ON i.location_id = r.location_id
WHERE i.part_id = '{partId}'
  AND (i.quantity_on_hand - COALESCE(r.reserved_qty, 0)) > 0
ORDER BY available DESC;
```

---

### Strategy 4: Customer Communication & Partial Fulfillment

**When:** Backorder cannot be resolved quickly

**Action Plan:**
1. **Immediate:** Notify customer of backorder within 24 hours
2. **Offer Partial Fulfillment:** Ship available quantity now, backorder remainder
3. **Provide ETA:** Based on incoming PO expected delivery date
4. **Offer Alternatives:** Substitute products if available
5. **Discount Consideration:** Goodwill gesture for inconvenience

**Email Template:**
```
Subject: Order #{orderId} - Partial Shipment Update

Dear Customer,

Thank you for your order of {orderedQty} units of {partName}.

We currently have {availableQty} units in stock and are shipping them today.
The remaining {backorderQty} units are expected to arrive from our supplier
on {expectedDate} and will ship immediately upon receipt.

We apologize for any inconvenience. As a gesture of goodwill, we're applying
a {discount}% discount to the backordered units.

Tracking information will be sent separately.

Best regards,
CloudWatcher Team
```

---

## Database Queries

### Query 1: Identify Backordered Orders

```sql
WITH inventory_totals AS (
  SELECT part_id, SUM(quantity_on_hand) as total_on_hand
  FROM inventory
  GROUP BY part_id
)
SELECT 
  o.id as order_id,
  o.request_id,
  oi.part_id,
  p.code as part_code,
  p.name as part_name,
  oi.quantity as ordered,
  it.total_on_hand as available,
  oi.quantity - it.total_on_hand as backorder_qty,
  o.created_at
FROM orders o
JOIN order_items oi ON o.id = oi.order_id
JOIN parts p ON oi.part_id = p.id
JOIN inventory_totals it ON oi.part_id = it.part_id
WHERE o.status IN ('pending', 'approved')
  AND oi.quantity > it.total_on_hand
ORDER BY o.created_at;
```

---

### Query 2: Backorder Impact by Part

```sql
SELECT 
  p.code,
  p.name,
  COUNT(DISTINCT o.id) as affected_orders,
  SUM(GREATEST(0, oi.quantity - i.total_on_hand)) as total_backorder_qty,
  AVG(GREATEST(0, oi.quantity - i.total_on_hand)) as avg_backorder_per_order
FROM parts p
JOIN order_items oi ON p.id = oi.part_id
JOIN orders o ON oi.order_id = o.id
JOIN (
  SELECT part_id, SUM(quantity_on_hand) as total_on_hand
  FROM inventory
  GROUP BY part_id
) i ON p.id = i.part_id
WHERE o.status IN ('pending', 'approved')
  AND oi.quantity > i.total_on_hand
GROUP BY p.id, p.code, p.name
ORDER BY total_backorder_qty DESC;
```

---

### Query 3: Backorder Resolution Timeline

```sql
WITH backorders AS (
  SELECT 
    oi.part_id,
    SUM(GREATEST(0, oi.quantity - i.on_hand)) as backorder_qty
  FROM order_items oi
  JOIN orders o ON oi.order_id = o.id
  JOIN (
    SELECT part_id, SUM(quantity_on_hand) as on_hand
    FROM inventory GROUP BY part_id
  ) i ON oi.part_id = i.part_id
  WHERE o.status IN ('pending', 'approved')
  GROUP BY oi.part_id
  HAVING SUM(GREATEST(0, oi.quantity - i.on_hand)) > 0
),
incoming AS (
  SELECT 
    poi.part_id,
    SUM(poi.quantity_ordered - poi.quantity_received) as incoming_qty,
    MIN(po.expected_delivery_date) as earliest_delivery
  FROM purchase_order_items poi
  JOIN purchase_orders po ON poi.purchase_order_id = po.id
  WHERE po.status = 'approved'
    AND NOT po.is_fully_received
  GROUP BY poi.part_id
)
SELECT 
  p.code,
  p.name,
  b.backorder_qty,
  COALESCE(inc.incoming_qty, 0) as incoming_qty,
  b.backorder_qty - COALESCE(inc.incoming_qty, 0) as shortfall,
  inc.earliest_delivery,
  CASE 
    WHEN COALESCE(inc.incoming_qty, 0) >= b.backorder_qty 
      THEN 'Resolvable'
    ELSE 'Needs Additional PO'
  END as resolution_status
FROM parts p
JOIN backorders b ON p.id = b.part_id
LEFT JOIN incoming inc ON p.id = inc.part_id
ORDER BY shortfall DESC;
```

---

## Automation & Alerts

### Alert Configuration

#### Alert 1: Backorder Detected
**Trigger:** `backorderedUnits > 0`

**Actions:**
1. Send email to inventory manager
2. Create task in task management system
3. Log event in audit trail
4. Update part status to 'backordered'

**Email Template:**
```
Subject: ⚠️ BACKORDER ALERT - {partCode}

Part: {partCode} - {partName}
Backorder Quantity: {backorderQty} units

Current Status:
- On Hand: {onHand}
- Reserved: {reserved}
- Incoming: {incoming}

Recommended Action:
{recommendation}

View Details: {apiUrl}
```

---

#### Alert 2: Persistent Backorder
**Trigger:** `backorderedUnits > 0` for more than 48 hours

**Actions:**
1. Escalate to operations manager
2. Flag for executive review
3. Contact supplier for status update
4. Initiate customer communication plan

---

#### Alert 3: Incoming Shipment Insufficient
**Trigger:** `effectiveAvailableUnits < 0`

**Interpretation:** Even after incoming shipments, backorder persists

**Actions:**
1. Emergency PO required
2. Notify procurement team
3. Evaluate alternative suppliers
4. Consider product substitutions

---

### Automated Backorder Resolution

#### Script: Auto-Create Emergency PO

```python
import requests

def auto_create_emergency_po(part_id):
    # Check availability
    availability = requests.get(
        f"http://localhost:5000/api/v2/inventory/{part_id}/availability",
        headers={"X-Api-Key": "dev-local-key"}
    ).json()
    
    backorder = availability['backorderedUnits']
    incoming = availability['incomingUnits']
    
    # Calculate needed quantity
    shortfall = max(0, backorder - incoming)
    
    if shortfall > 0:
        # Add safety margin (20%)
        po_quantity = int(shortfall * 1.2)
        
        # Create PO (pseudo-code)
        create_purchase_order(
            part_id=part_id,
            quantity=po_quantity,
            priority='urgent',
            reason=f'Backorder resolution - {backorder} units backordered'
        )
        
        print(f"✅ Created emergency PO for {po_quantity} units")
    else:
        print(f"✓ Incoming shipments cover backorder")
```

---

## Best Practices

### 1. **Prevent Backorders**
- Don't approve orders without checking availability first
- Maintain safety stock levels above reorder point
- Monitor incoming shipment status daily
- Set up low-stock alerts

### 2. **Rapid Detection**
- Monitor availability API hourly
- Set up real-time alerts for `backorderedUnits > 0`
- Dashboard showing backorder KPIs

### 3. **Swift Resolution**
- Respond to backorders within 24 hours
- Maintain list of alternative suppliers
- Have expedited shipping agreements in place

### 4. **Customer Communication**
- Notify customers immediately when backorder detected
- Provide accurate ETAs based on incoming PO dates
- Offer alternatives or partial fulfillment

### 5. **Root Cause Analysis**
- Track backorder frequency by part
- Identify patterns (seasonal, supplier issues, forecasting errors)
- Adjust safety stock levels based on backorder history

---

## Troubleshooting

### Issue: Backorder Reported but Stock Visible in Warehouse

**Cause:** Location mismatch - inventory exists but at different location than order

**Solution:**
1. Check `locations` array in availability response
2. Verify OrderItem has correct `location_id`
3. Transfer stock to correct location or update order location

**Query:**
```sql
-- Find location mismatch
SELECT 
  oi.id as order_item_id,
  oi.location_id as requested_location,
  i.location_id as stock_location,
  i.quantity_on_hand
FROM order_items oi
JOIN inventory i ON oi.part_id = i.part_id
WHERE oi.id = '{orderItemId}'
  AND i.quantity_on_hand > 0;
```

---

### Issue: Incoming PO Not Reducing Backorder

**Cause:** PO status not 'approved' or `is_fully_received = true`

**Solution:**
```sql
-- Check PO status
SELECT po.id, po.status, po.is_fully_received, poi.quantity_ordered, poi.quantity_received
FROM purchase_orders po
JOIN purchase_order_items poi ON po.id = poi.purchase_order_id
WHERE poi.part_id = '{partId}';

-- Fix if needed
UPDATE purchase_orders 
SET status = 'approved', is_fully_received = false
WHERE id = '{poId}';
```

---

### Issue: Effective Availability Negative

**Scenario:**
```json
{
  "totalQuantityOnHand": 50,
  "reservedUnits": 100,
  "incomingUnits": 20,
  "effectiveAvailableUnits": -30
}
```

**Interpretation:** Even after incoming shipment of 20, you're still 30 units short

**Action:**
1. Create emergency PO for at least 30 units (recommend 50 for safety)
2. Prioritize partial fulfillment for existing orders
3. Communicate delays to customers
4. Investigate why orders were approved without stock verification

---

## Conclusion

Effective backorder management requires:
- **Prevention:** Proper stock levels and order approval processes
- **Detection:** Real-time monitoring via availability API
- **Resolution:** Swift action with expedited POs or stock transfers
- **Communication:** Transparent customer updates

Use the queries and strategies in this guide to minimize backorder impact and maintain customer satisfaction.

**For Technical Support:** Contact the CloudWatcher development team  
**For Supplier Issues:** Escalate to procurement team

---

**Version:** 2.0 (Wave 4 Release)  
**Last Updated:** December 2025  
**Related Guides:** [User Guide - Availability](User-Guide-Availability.md) | [Deployment Guide](Deployment-Wave4.md)
