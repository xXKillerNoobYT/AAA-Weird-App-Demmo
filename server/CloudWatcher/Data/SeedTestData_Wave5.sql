-- ==========================================
-- Wave 5: Order Management Seed Data
-- ==========================================
-- Purpose: Test data for POST, GET, PATCH /api/v2/orders endpoints
-- Date: 2025-12-25
--
-- This script creates:
-- - 1 test user
-- - 3 test parts
-- - 5 orders with various statuses
-- - Order items for each order
-- - Location references for inventory tracking
-- ==========================================

-- Clean up existing test data (if any)
DELETE FROM "OrderHistory" WHERE "OrderId" IN (SELECT "Id" FROM "Orders" WHERE "TotalAmount" < 1000);
DELETE FROM "OrderItems" WHERE "OrderId" IN (SELECT "Id" FROM "Orders" WHERE "TotalAmount" < 1000);
DELETE FROM "OrderApprovals" WHERE "OrderId" IN (SELECT "Id" FROM "Orders" WHERE "TotalAmount" < 1000);
DELETE FROM "Orders" WHERE "TotalAmount" < 1000;

-- ==========================================
-- 1. Test User
-- ==========================================
-- Email: testuser@cloudwatcher.local
-- OAuthId: test-oauth-001
INSERT INTO "Users" ("Id", "Email", "OAuthId", "FirstName", "LastName", "PhoneNumber", "IsActive", "CreatedAt")
VALUES (
    '111e8400-e29b-41d4-a716-446655440000'::uuid,
    'testuser@cloudwatcher.local',
    'test-oauth-001',
    'Test',
    'User',
    '+15551234567',
    true,
    NOW()
)
ON CONFLICT ("Email") DO NOTHING;

-- ==========================================
-- 2. Test Parts
-- ==========================================
-- Part 1: Widget Assembly (Low Price)
INSERT INTO "Parts" ("Id", "Code", "Name", "Description", "UnitPrice", "MinOrderQty", "CreatedAt")
VALUES (
    '222e8400-e29b-41d4-a716-446655440000'::uuid,
    'WID-ASSEMBLY-001',
    'Widget Assembly - Basic',
    'Standard widget assembly for testing order management',
    25.50,
    1,
    NOW()
)
ON CONFLICT ("Code") DO NOTHING;

-- Part 2: Gadget Component (Medium Price)
INSERT INTO "Parts" ("Id", "Code", "Name", "Description", "UnitPrice", "MinOrderQty", "CreatedAt")
VALUES (
    '333e8400-e29b-41d4-a716-446655440000'::uuid,
    'GAD-COMPONENT-002',
    'Gadget Component - Premium',
    'Premium gadget component with extended warranty',
    75.00,
    5,
    NOW()
)
ON CONFLICT ("Code") DO NOTHING;

-- Part 3: Hardware Kit (High Price)
INSERT INTO "Parts" ("Id", "Code", "Name", "Description", "UnitPrice", "MinOrderQty", "CreatedAt")
VALUES (
    '444e8400-e29b-41d4-a716-446655440000'::uuid,
    'HW-KIT-DELUXE-003',
    'Hardware Kit - Deluxe',
    'Complete hardware kit with tools and fasteners',
    150.00,
    1,
    NOW()
)
ON CONFLICT ("Code") DO NOTHING;

-- ==========================================
-- 3. Test Locations
-- ==========================================
INSERT INTO "Locations" ("Id", "Name", "Type", "Address", "IsActive", "CreatedAt")
VALUES
    ('555e8400-e29b-41d4-a716-446655440000'::uuid, 'Main Warehouse - Test', 'warehouse', '100 Test St, City, ST 12345', true, NOW()),
    ('666e8400-e29b-41d4-a716-446655440000'::uuid, 'Retail Store - Test', 'retail', '200 Test Ave, City, ST 12345', true, NOW()),
    ('777e8400-e29b-41d4-a716-446655440000'::uuid, 'Service Van #1 - Test', 'mobile', 'Mobile Service Area', true, NOW())
ON CONFLICT DO NOTHING;

-- ==========================================
-- 4. Orders with Different Statuses
-- ==========================================

-- Order 1: Pending (ready for approval)
-- Status: pending
-- Items: 2 widgets
INSERT INTO "Orders" ("Id", "RequestId", "Status", "TotalAmount", "CreatedAt")
VALUES (
    '801e8400-e29b-41d4-a716-446655440000'::uuid,
    NULL,
    'pending',
    51.00, -- 2 * 25.50
    NOW() - INTERVAL '1 day'
);

INSERT INTO "OrderItems" ("Id", "OrderId", "PartId", "LocationId", "Quantity", "UnitPrice", "LineAmount")
VALUES (
    '8a1e8400-e29b-41d4-a716-446655440000'::uuid,
    '801e8400-e29b-41d4-a716-446655440000'::uuid,
    '222e8400-e29b-41d4-a716-446655440000'::uuid, -- Widget Assembly
    '555e8400-e29b-41d4-a716-446655440000'::uuid, -- Main Warehouse
    2,
    25.50,
    51.00
);

-- Order 2: Approved (ready for shipping)
-- Status: approved
-- Items: 10 gadgets
INSERT INTO "Orders" ("Id", "RequestId", "Status", "TotalAmount", "CreatedAt")
VALUES (
    '802e8400-e29b-41d4-a716-446655440000'::uuid,
    NULL,
    'approved',
    750.00, -- 10 * 75.00
    NOW() - INTERVAL '2 days'
);

INSERT INTO "OrderItems" ("Id", "OrderId", "PartId", "LocationId", "Quantity", "UnitPrice", "LineAmount")
VALUES (
    '8a2e8400-e29b-41d4-a716-446655440000'::uuid,
    '802e8400-e29b-41d4-a716-446655440000'::uuid,
    '333e8400-e29b-41d4-a716-446655440000'::uuid, -- Gadget Component
    '666e8400-e29b-41d4-a716-446655440000'::uuid, -- Retail Store
    10,
    75.00,
    750.00
);

-- Order 3: Shipped (in transit)
-- Status: shipped
-- Items: 1 widget + 1 hardware kit
INSERT INTO "Orders" ("Id", "RequestId", "Status", "TotalAmount", "CreatedAt", "ShippedAt")
VALUES (
    '803e8400-e29b-41d4-a716-446655440000'::uuid,
    NULL,
    'shipped',
    175.50, -- 25.50 + 150.00
    NOW() - INTERVAL '5 days',
    NOW() - INTERVAL '1 day'
);

INSERT INTO "OrderItems" ("Id", "OrderId", "PartId", "LocationId", "Quantity", "UnitPrice", "LineAmount")
VALUES
    ('8a3e8400-e29b-41d4-a716-446655440000'::uuid,
     '803e8400-e29b-41d4-a716-446655440000'::uuid,
     '222e8400-e29b-41d4-a716-446655440000'::uuid, -- Widget
     '777e8400-e29b-41d4-a716-446655440000'::uuid, -- Service Van
     1,
     25.50,
     25.50),
    ('8a4e8400-e29b-41d4-a716-446655440000'::uuid,
     '803e8400-e29b-41d4-a716-446655440000'::uuid,
     '444e8400-e29b-41d4-a716-446655440000'::uuid, -- Hardware Kit
     '777e8400-e29b-41d4-a716-446655440000'::uuid, -- Service Van
     1,
     150.00,
     150.00);

-- Order 4: Delivered (complete)
-- Status: delivered
-- Items: 20 widgets
INSERT INTO "Orders" ("Id", "RequestId", "Status", "TotalAmount", "CreatedAt", "ShippedAt", "DeliveredAt")
VALUES (
    '804e8400-e29b-41d4-a716-446655440000'::uuid,
    NULL,
    'delivered',
    510.00, -- 20 * 25.50
    NOW() - INTERVAL '10 days',
    NOW() - INTERVAL '7 days',
    NOW() - INTERVAL '5 days'
);

INSERT INTO "OrderItems" ("Id", "OrderId", "PartId", "LocationId", "Quantity", "UnitPrice", "LineAmount")
VALUES (
    '8a5e8400-e29b-41d4-a716-446655440000'::uuid,
    '804e8400-e29b-41d4-a716-446655440000'::uuid,
    '222e8400-e29b-41d4-a716-446655440000'::uuid, -- Widget
    '555e8400-e29b-41d4-a716-446655440000'::uuid, -- Warehouse
    20,
    25.50,
    510.00
);

-- Order 5: Cancelled (user cancelled)
-- Status: cancelled
-- Items: 5 gadgets
INSERT INTO "Orders" ("Id", "RequestId", "Status", "TotalAmount", "CreatedAt")
VALUES (
    '805e8400-e29b-41d4-a716-446655440000'::uuid,
    NULL,
    'cancelled',
    375.00, -- 5 * 75.00
    NOW() - INTERVAL '3 days'
);

INSERT INTO "OrderItems" ("Id", "OrderId", "PartId", "LocationId", "Quantity", "UnitPrice", "LineAmount")
VALUES (
    '8a6e8400-e29b-41d4-a716-446655440000'::uuid,
    '805e8400-e29b-41d4-a716-446655440000'::uuid,
    '333e8400-e29b-41d4-a716-446655440000'::uuid, -- Gadget
    '666e8400-e29b-41d4-a716-446655440000'::uuid, -- Retail Store
    5,
    75.00,
    375.00
);

-- ==========================================
-- 5. Order Approvals
-- ==========================================
-- Order 2 was approved by test user
INSERT INTO "OrderApprovals" ("Id", "OrderId", "ApproverId", "Status", "Notes", "RequestedAt", "ApprovedAt")
VALUES (
    '901e8400-e29b-41d4-a716-446655440000'::uuid,
    '802e8400-e29b-41d4-a716-446655440000'::uuid,
    '111e8400-e29b-41d4-a716-446655440000'::uuid, -- Test User
    'approved',
    'Approved for immediate shipment - high priority customer',
    NOW() - INTERVAL '2 days',
    NOW() - INTERVAL '2 days'
);

-- ==========================================
-- 6. Order History
-- ==========================================
INSERT INTO "OrderHistory" ("Id", "OrderId", "Event", "UserId", "Details", "Timestamp")
VALUES
    -- Order 1 history (pending)
    ('a01e8400-e29b-41d4-a716-446655440000'::uuid,
     '801e8400-e29b-41d4-a716-446655440000'::uuid,
     'created',
     '111e8400-e29b-41d4-a716-446655440000'::uuid,
     'Order created with 1 items, total $51.00',
     NOW() - INTERVAL '1 day'),

    -- Order 2 history (approved)
    ('a02e8400-e29b-41d4-a716-446655440000'::uuid,
     '802e8400-e29b-41d4-a716-446655440000'::uuid,
     'created',
     '111e8400-e29b-41d4-a716-446655440000'::uuid,
     'Order created with 1 items, total $750.00',
     NOW() - INTERVAL '2 days'),
    ('a03e8400-e29b-41d4-a716-446655440000'::uuid,
     '802e8400-e29b-41d4-a716-446655440000'::uuid,
     'approved',
     '111e8400-e29b-41d4-a716-446655440000'::uuid,
     'Order approved by user 111e8400-e29b-41d4-a716-446655440000. Notes: Approved for immediate shipment - high priority customer',
     NOW() - INTERVAL '2 days'),

    -- Order 3 history (shipped)
    ('a04e8400-e29b-41d4-a716-446655440000'::uuid,
     '803e8400-e29b-41d4-a716-446655440000'::uuid,
     'created',
     '111e8400-e29b-41d4-a716-446655440000'::uuid,
     'Order created with 2 items, total $175.50',
     NOW() - INTERVAL '5 days'),
    ('a05e8400-e29b-41d4-a716-446655440000'::uuid,
     '803e8400-e29b-41d4-a716-446655440000'::uuid,
     'status_changed_shipped',
     NULL,
     'Status changed from approved to shipped. Notes: None',
     NOW() - INTERVAL '1 day'),

    -- Order 4 history (delivered)
    ('a06e8400-e29b-41d4-a716-446655440000'::uuid,
     '804e8400-e29b-41d4-a716-446655440000'::uuid,
     'created',
     '111e8400-e29b-41d4-a716-446655440000'::uuid,
     'Order created with 1 items, total $510.00',
     NOW() - INTERVAL '10 days'),
    ('a07e8400-e29b-41d4-a716-446655440000'::uuid,
     '804e8400-e29b-41d4-a716-446655440000'::uuid,
     'status_changed_delivered',
     NULL,
     'Status changed from shipped to delivered. Notes: None',
     NOW() - INTERVAL '5 days'),

    -- Order 5 history (cancelled)
    ('a08e8400-e29b-41d4-a716-446655440000'::uuid,
     '805e8400-e29b-41d4-a716-446655440000'::uuid,
     'created',
     '111e8400-e29b-41d4-a716-446655440000'::uuid,
     'Order created with 1 items, total $375.00',
     NOW() - INTERVAL '3 days'),
    ('a09e8400-e29b-41d4-a716-446655440000'::uuid,
     '805e8400-e29b-41d4-a716-446655440000'::uuid,
     'status_changed_cancelled',
     '111e8400-e29b-41d4-a716-446655440000'::uuid,
     'Status changed from pending to cancelled. Notes: Customer requested cancellation',
     NOW() - INTERVAL '3 days');

-- ==========================================
-- Verification Queries
-- ==========================================

-- Count orders by status
SELECT "Status", COUNT(*) AS count, SUM("TotalAmount") AS total_amount
FROM "Orders"
GROUP BY "Status"
ORDER BY "Status";

-- List all test orders with item counts
SELECT
    o."Id",
    o."Status",
    o."TotalAmount",
    COUNT(oi."Id") AS item_count,
    o."CreatedAt",
    o."ShippedAt",
    o."DeliveredAt"
FROM "Orders" o
LEFT JOIN "OrderItems" oi ON o."Id" = oi."OrderId"
WHERE o."TotalAmount" < 1000 -- Test data filter
GROUP BY o."Id", o."Status", o."TotalAmount", o."CreatedAt", o."ShippedAt", o."DeliveredAt"
ORDER BY o."CreatedAt" DESC;

-- ==========================================
-- Expected Results Summary
-- ==========================================
-- 5 orders total:
-- - 1 pending  (Order 1: $51.00)
-- - 1 approved (Order 2: $750.00)
-- - 1 shipped  (Order 3: $175.50)
-- - 1 delivered (Order 4: $510.00)
-- - 1 cancelled (Order 5: $375.00)
--
-- Total order value: $1,861.50
-- ==========================================
