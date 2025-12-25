-- Wave 4 Availability Enrichment - Test Data Seed Script
-- Purpose: Seed database with sample data to test availability calculations
-- Date: 2025-12-25

-- Clean up existing test data (if any)
DELETE FROM purchase_order_items WHERE purchase_order_id IN (SELECT id FROM purchase_orders WHERE supplier_id = 'bb6h3a66-k85h-96i9-f26b-aa2cc3545555');
DELETE FROM purchase_orders WHERE supplier_id = 'bb6h3a66-k85h-96i9-f26b-aa2cc3545555';
DELETE FROM order_items WHERE order_id IN (SELECT id FROM orders WHERE id IN ('994f1844-i63f-74g7-d049-8800a1323333', 'aa5g2955-j74g-85h8-e15a-991bb2434444'));
DELETE FROM orders WHERE id IN ('994f1844-i63f-74g7-d049-8800a1323333', 'aa5g2955-j74g-85h8-e15a-991bb2434444');
DELETE FROM inventory WHERE part_id = '550e8400-e29b-41d4-a716-446655440000';
DELETE FROM suppliers WHERE id = 'bb6h3a66-k85h-96i9-f26b-aa2cc3545555';
DELETE FROM locations WHERE id IN ('661e8511-f30c-41d4-a716-557788990000', '772f9622-g41d-52e5-b827-668899101111', '883g0733-h52e-63f6-c938-779900212222');
DELETE FROM parts WHERE id = '550e8400-e29b-41d4-a716-446655440000';

-- Insert test part
INSERT INTO parts (id, code, name, category, unit_price, created_at, modified_at) VALUES
('550e8400-e29b-41d4-a716-446655440000', 'PART-001', 'Test Widget Alpha', 'Electronics', 25.00, NOW(), NOW());

-- Insert test locations
INSERT INTO locations (id, name, type, address, created_at) VALUES
('661e8511-f30c-41d4-a716-557788990000', 'Main Warehouse', 'warehouse', '123 Storage St', NOW()),
('772f9622-g41d-52e5-b827-668899101111', 'Retail Store Downtown', 'retail', '456 Main Ave', NOW()),
('883g0733-h52e-63f6-c938-779900212222', 'Mobile Service Truck 1', 'truck', 'Mobile Unit', NOW());

-- Insert inventory records
INSERT INTO inventory (id, part_id, location_id, quantity_on_hand, reorder_level, reorder_quantity, last_inventory_check, created_at, modified_at) VALUES
(gen_random_uuid(), '550e8400-e29b-41d4-a716-446655440000', '661e8511-f30c-41d4-a716-557788990000', 50, 20, 100, NOW(), NOW(), NOW()),
(gen_random_uuid(), '550e8400-e29b-41d4-a716-446655440000', '772f9622-g41d-52e5-b827-668899101111', 30, 10, 50, NOW(), NOW(), NOW()),
(gen_random_uuid(), '550e8400-e29b-41d4-a716-446655440000', '883g0733-h52e-63f6-c938-779900212222', 20, 5, 25, NOW(), NOW(), NOW());

-- Total inventory: 100 units across 3 locations

-- Insert test orders (for reserved quantity calculation)
INSERT INTO orders (id, request_id, status, total_amount, created_at) VALUES
('994f1844-i63f-74g7-d049-8800a1323333', NULL, 'pending', 375.00, NOW()),
('aa5g2955-j74g-85h8-e15a-991bb2434444', NULL, 'approved', 500.00, NOW());

-- Insert order items (reserved quantities)
-- Warehouse: 15 units reserved (pending)
-- Retail: 20 units reserved (approved)
-- Total reserved: 35 units
INSERT INTO order_items (id, order_id, part_id, location_id, quantity, unit_price, line_amount) VALUES
(gen_random_uuid(), '994f1844-i63f-74g7-d049-8800a1323333', '550e8400-e29b-41d4-a716-446655440000', '661e8511-f30c-41d4-a716-557788990000', 15, 25.00, 375.00),
(gen_random_uuid(), 'aa5g2955-j74g-85h8-e15a-991bb2434444', '550e8400-e29b-41d4-a716-446655440000', '772f9622-g41d-52e5-b827-668899101111', 20, 25.00, 500.00);

-- Insert test supplier
INSERT INTO suppliers (id, name, email, phone, address, rating, is_preferred, created_at, modified_at) VALUES
('bb6h3a66-k85h-96i9-f26b-aa2cc3545555', 'Widget Supply Co', 'orders@widgetsupply.com', '555-1234', '789 Industrial Rd', 4.5, true, NOW(), NOW());

-- Insert purchase order (for incoming inventory calculation)
INSERT INTO purchase_orders (id, supplier_id, status, total_amount, order_date, expected_delivery_date, is_fully_received) VALUES
('cc7i4b77-l96i-a7ja-g37c-bb3dd4656666', 'bb6h3a66-k85h-96i9-f26b-aa2cc3545555', 'approved', 2500.00, NOW(), NOW() + INTERVAL '7 days', false);

-- Insert purchase order items (incoming quantities)
-- Ordered: 100 units, Received: 0
-- Incoming: 100 units
INSERT INTO purchase_order_items (id, purchase_order_id, part_id, quantity_ordered, quantity_received, unit_cost, line_amount) VALUES
(gen_random_uuid(), 'cc7i4b77-l96i-a7ja-g37c-bb3dd4656666', '550e8400-e29b-41d4-a716-446655440000', 100, 0, 25.00, 2500.00);

-- Expected Availability Calculation:
-- TotalQuantityOnHand: 100 (50+30+20)
-- ReservedUnits: 35 (15+20)
-- TotalAvailable: 65 (100-35)
-- IncomingUnits: 100 (100-0)
-- BackorderedUnits: 0 (reserved < onHand)
-- EffectiveAvailableUnits: 165 (65-0+100)

-- Test Query
SELECT 
    'Test Data Seeded Successfully' as message,
    COUNT(*) as inventory_records,
    SUM(quantity_on_hand) as total_on_hand
FROM inventory 
WHERE part_id = '550e8400-e29b-41d4-a716-446655440000';

-- Verify Reserved Calculation
SELECT 
    'Reserved Units Check' as message,
    SUM(oi.quantity) as total_reserved
FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE oi.part_id = '550e8400-e29b-41d4-a716-446655440000'
  AND o.status IN ('pending', 'approved');

-- Verify Incoming Calculation
SELECT 
    'Incoming Units Check' as message,
    SUM(poi.quantity_ordered - poi.quantity_received) as total_incoming
FROM purchase_order_items poi
JOIN purchase_orders po ON poi.purchase_order_id = po.id
WHERE poi.part_id = '550e8400-e29b-41d4-a716-446655440000'
  AND po.status = 'approved'
  AND po.is_fully_received = false;
