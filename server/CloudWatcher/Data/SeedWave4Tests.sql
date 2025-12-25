-- Wave 4 Test Data Seeding Script
-- Purpose: Create test data for availability endpoint testing
-- Expected: Part PART-001 with 100 units total, 35 reserved, 50 incoming

BEGIN;

-- Clean up existing test data
DELETE FROM "OrderItems" WHERE "OrderId" IN (
    SELECT "Id" FROM "Orders" WHERE "CustomerName" LIKE '%Test Customer%'
);
DELETE FROM "Orders" WHERE "CustomerName" LIKE '%Test Customer%';

DELETE FROM "PurchaseOrderItems" WHERE "PurchaseOrderId" IN (
    SELECT "Id" FROM "PurchaseOrders" WHERE "VendorName" LIKE '%Test Supplier%'
);
DELETE FROM "PurchaseOrders" WHERE "VendorName" LIKE '%Test Supplier%';

DELETE FROM "Inventory" WHERE "PartId" IN (
    SELECT "Id" FROM "Parts" WHERE "Code" = 'PART-001'
);
DELETE FROM "Parts" WHERE "Code" = 'PART-001';

DELETE FROM "Locations" WHERE "Name" IN ('Test Warehouse', 'Test Retail Store', 'Test Delivery Truck');

-- Insert Test Part
INSERT INTO "Parts" ("Id", "Code", "Name", "Description", "Category", "StandardPrice", "CreatedAt")
VALUES (
    '550e8400-e29b-41d4-a716-446655440000',
    'PART-001',
    'Test Widget Alpha',
    'High-quality test widget for Wave 4 validation',
    'Test Equipment',
    29.99,
    CURRENT_TIMESTAMP
);

-- Insert Test Locations
INSERT INTO "Locations" ("Id", "Name", "Address", "IsActive", "CreatedAt")
VALUES 
    ('661e8511-f30c-41d4-a716-557788990000', 'Test Warehouse', '123 Storage Blvd', true, CURRENT_TIMESTAMP),
    ('772f9622-041d-52e5-b827-668899101111', 'Test Retail Store', '456 Main Street', true, CURRENT_TIMESTAMP),
    ('883f0733-152e-63f6-c938-779900212222', 'Test Delivery Truck', 'Mobile Unit A', true, CURRENT_TIMESTAMP);

-- Insert Inventory Records (Total: 100 units)
INSERT INTO "Inventory" ("Id", "PartId", "LocationId", "QuantityOnHand", "ReorderLevel", "ReorderQuantity", "LastInventoryCheck")
VALUES 
    (gen_random_uuid(), '550e8400-e29b-41d4-a716-446655440000', '661e8511-f30c-41d4-a716-557788990000', 50, 20, 30, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '550e8400-e29b-41d4-a716-446655440000', '772f9622-041d-52e5-b827-668899101111', 30, 10, 20, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '550e8400-e29b-41d4-a716-446655440000', '883f0733-152e-63f6-c938-779900212222', 20, 5, 15, CURRENT_TIMESTAMP);

-- Insert Test Orders (Reserved: 35 units)
-- Order 1: Pending (15 units reserved)
INSERT INTO "Orders" ("Id", "CustomerName", "Status", "CreatedAt")
VALUES ('994f1844-163f-74g7-d049-8800a1323333', 'Test Customer A', 'pending', CURRENT_TIMESTAMP);

INSERT INTO "OrderItems" ("Id", "OrderId", "PartId", "Quantity", "UnitPrice")
VALUES (gen_random_uuid(), '994f1844-163f-74g7-d049-8800a1323333', '550e8400-e29b-41d4-a716-446655440000', 15, 29.99);

-- Order 2: Approved (20 units reserved)
INSERT INTO "Orders" ("Id", "CustomerName", "Status", "CreatedAt")
VALUES ('aa5f2955-274g-85h8-e15a-991bb2434444', 'Test Customer B', 'approved', CURRENT_TIMESTAMP);

INSERT INTO "OrderItems" ("Id", "OrderId", "PartId", "Quantity", "UnitPrice")
VALUES (gen_random_uuid(), 'aa5f2955-274g-85h8-e15a-991bb2434444', '550e8400-e29b-41d4-a716-446655440000', 20, 29.99);

-- Insert Test Supplier
INSERT INTO "Suppliers" ("Id", "Name", "ContactEmail", "ContactPhone", "IsActive", "CreatedAt")
VALUES ('bb6f3a66-385h-96i9-f26b-aa2cc3545555', 'Test Supplier Inc', 'supplier@test.com', '555-TEST-001', true, CURRENT_TIMESTAMP);

-- Insert Test Purchase Order (Incoming: 50 units)
INSERT INTO "PurchaseOrders" ("Id", "VendorName", "SupplierId", "Status", "OrderedAt")
VALUES ('cc7f4b77-496i-a7j0-g37c-bb3dd4656666', 'Test Supplier Inc', 'bb6f3a66-385h-96i9-f26b-aa2cc3545555', 'approved', CURRENT_TIMESTAMP);

INSERT INTO "PurchaseOrderItems" ("Id", "PurchaseOrderId", "PartId", "QuantityOrdered", "QuantityReceived", "UnitCost", "LineAmount")
VALUES (gen_random_uuid(), 'cc7f4b77-496i-a7j0-g37c-bb3dd4656666', '550e8400-e29b-41d4-a716-446655440000', 50, 0, 19.99, 999.50);

COMMIT;

-- Verification Queries
SELECT 
    '✅ Seeding complete' AS status,
    'PART-001' AS part_code,
    (SELECT SUM("QuantityOnHand") FROM "Inventory" WHERE "PartId" = '550e8400-e29b-41d4-a716-446655440000') AS total_on_hand,
    (SELECT SUM(oi."Quantity") FROM "OrderItems" oi 
     JOIN "Orders" o ON oi."OrderId" = o."Id" 
     WHERE oi."PartId" = '550e8400-e29b-41d4-a716-446655440000' 
     AND o."Status" IN ('pending', 'approved')) AS reserved_units,
    (SELECT SUM(poi."QuantityOrdered" - poi."QuantityReceived") FROM "PurchaseOrderItems" poi
     JOIN "PurchaseOrders" po ON poi."PurchaseOrderId" = po."Id"
     WHERE poi."PartId" = '550e8400-e29b-41d4-a716-446655440000'
     AND po."Status" = 'approved') AS incoming_units;
