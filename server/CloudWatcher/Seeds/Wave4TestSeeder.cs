using CloudWatcher.Data;
using CloudWatcher.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudWatcher.Seeds
{
    /// <summary>
    /// Wave 4 Test Data Seeder - populates sample data for availability testing
    /// Matches actual CloudWatcher model properties (Parts.cs, Orders.cs)
    /// </summary>
    public class Wave4TestSeeder
    {
        private readonly CloudWatcherContext _context;

        // Test GUIDs for consistent reference
        private static readonly Guid TestPartId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        private static readonly Guid WarehouseId = Guid.Parse("661e8511-f30c-41d4-a716-557788990000");
        private static readonly Guid RetailId = Guid.Parse("772f9622-041d-52e5-b827-668899101111");
        private static readonly Guid TruckId = Guid.Parse("883f0733-152e-63f6-c938-779900212222");
        private static readonly Guid SupplierId = Guid.Parse("bb6f3a66-385e-96f9-f26b-aa2cc3545555");
        private static readonly Guid PendingOrderId = Guid.Parse("994f1844-163f-74a7-d049-8800a1323333");
        private static readonly Guid ApprovedOrderId = Guid.Parse("aa5f2955-274a-85b8-e15a-991bb2434444");
        private static readonly Guid POId = Guid.Parse("cc7f4b77-496c-a7d0-a37c-bb3dd4656666");

        public Wave4TestSeeder(CloudWatcherContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Seed Wave 4 test data
        /// </summary>
        public async Task SeedAsync()
        {
            try
            {
                Console.WriteLine("🌱 Seeding Wave 4 test data...");

                await CleanupExistingTestDataAsync();
                await SeedTestPartAsync();
                await SeedTestLocationsAsync();
                await SeedTestInventoryAsync();
                await SeedTestSupplierAsync();
                await SeedTestOrdersAsync();
                await SeedTestPurchaseOrdersAsync();
                
                await _context.SaveChangesAsync();
                
                Console.WriteLine("\n✅ Wave 4 test data seeded successfully!");
                Console.WriteLine("   📦 Part: PART-001 (Test Widget Alpha)");
                Console.WriteLine("   📊 Total inventory: 100 units across 3 locations");
                Console.WriteLine("   🔒 Reserved: 35 units (2 orders: pending + approved)");
                Console.WriteLine("   📥 Incoming: 50 units (1 approved PO)");
                Console.WriteLine("   ✨ Expected available: 115 units (100 - 35 + 50)");
                
                await VerifyDataAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error seeding Wave 4 test data: {ex.Message}");
                throw;
            }
        }

        private async Task CleanupExistingTestDataAsync()
        {
            Console.WriteLine("  🧹 Cleaning up existing test data...");

            // Delete in reverse dependency order to avoid FK violations
            
            // 1. Purchase Order Items
            var existingPOItems = await _context.PurchaseOrderItems
                .Where(poi => poi.PurchaseOrderId == POId)
                .ToListAsync();
            _context.PurchaseOrderItems.RemoveRange(existingPOItems);

            // 2. Purchase Orders
            var existingPO = await _context.PurchaseOrders.FindAsync(POId);
            if (existingPO != null)
                _context.PurchaseOrders.Remove(existingPO);

            // 3. Order Items
            var existingOrderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == PendingOrderId || oi.OrderId == ApprovedOrderId)
                .ToListAsync();
            _context.OrderItems.RemoveRange(existingOrderItems);

            // 4. Orders
            var existingOrders = await _context.Orders
                .Where(o => o.Id == PendingOrderId || o.Id == ApprovedOrderId)
                .ToListAsync();
            _context.Orders.RemoveRange(existingOrders);

            // 5. Inventory
            var existingInventory = await _context.Inventory
                .Where(i => i.PartId == TestPartId)
                .ToListAsync();
            _context.Inventory.RemoveRange(existingInventory);

            // 6. Supplier
            var existingSupplier = await _context.Suppliers.FindAsync(SupplierId);
            if (existingSupplier != null)
                _context.Suppliers.Remove(existingSupplier);

            // 7. Locations
            var existingLocations = await _context.Locations
                .Where(l => l.Id == WarehouseId || l.Id == RetailId || l.Id == TruckId)
                .ToListAsync();
            _context.Locations.RemoveRange(existingLocations);

            // 8. Part (last due to FKs)
            var existingPart = await _context.Parts.FindAsync(TestPartId);
            if (existingPart != null)
                _context.Parts.Remove(existingPart);

            await _context.SaveChangesAsync();
        }

        private async Task SeedTestPartAsync()
        {
            Console.WriteLine("  📦 Seeding test part...");

            // Part model properties: Id, Code, Name, Description, Category, StandardPrice, CreatedAt, UpdatedAt
            var part = new Part
            {
                Id = TestPartId,
                Code = "PART-001",
                Name = "Test Widget Alpha",
                Description = "High-quality test widget for Wave 4 availability validation",
                Category = "Test Equipment",
                StandardPrice = 29.99m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Parts.Add(part);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestLocationsAsync()
        {
            Console.WriteLine("  📍 Seeding test locations...");

            // Location model properties: Id, Name, DepartmentId, Address, IsActive, CreatedAt
            var locations = new List<Location>
            {
                new()
                {
                    Id = WarehouseId,
                    Name = "Test Warehouse",
                    Address = "123 Storage Blvd, Industrial Park",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = RetailId,
                    Name = "Test Retail Store",
                    Address = "456 Main Street, Downtown",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = TruckId,
                    Name = "Test Delivery Truck",
                    Address = "Mobile Unit Alpha",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Locations.AddRange(locations);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestInventoryAsync()
        {
            Console.WriteLine("  📊 Seeding test inventory (100 total units)...");

            // Inventory model properties: Id, PartId, LocationId, QuantityOnHand, ReorderLevel, ReorderQuantity, LastInventoryCheck
            var inventoryRecords = new List<Inventory>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PartId = TestPartId,
                    LocationId = WarehouseId,
                    QuantityOnHand = 50,
                    ReorderLevel = 20,
                    ReorderQuantity = 100,
                    LastInventoryCheck = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    PartId = TestPartId,
                    LocationId = RetailId,
                    QuantityOnHand = 30,
                    ReorderLevel = 10,
                    ReorderQuantity = 50,
                    LastInventoryCheck = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    PartId = TestPartId,
                    LocationId = TruckId,
                    QuantityOnHand = 20,
                    ReorderLevel = 5,
                    ReorderQuantity = 25,
                    LastInventoryCheck = DateTime.UtcNow
                }
            };

            _context.Inventory.AddRange(inventoryRecords);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestSupplierAsync()
        {
            Console.WriteLine("  🏭 Seeding test supplier...");

            // Supplier model properties: Id, Name, ContactEmail, ContactPhone, Address, IsActive, CreatedAt
            var supplier = new Supplier
            {
                Id = SupplierId,
                Name = "Test Supplier Inc",
                ContactEmail = "supplier@testwidgets.com",
                ContactPhone = "555-TEST-001",
                Address = "789 Industrial Road, Manufacturing District",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestOrdersAsync()
        {
            Console.WriteLine("  🛒 Seeding test orders (35 reserved units)...");

            // Order model properties: Id, RequestId, Status, TotalAmount, CreatedAt, CustomerName
            // OrderItem model properties: Id, OrderId, PartId, Quantity, UnitPrice
            
            // Order 1: Pending status (15 units reserved)
            var pendingOrder = new Order
            {
                Id = PendingOrderId,
                Status = "pending",
                TotalAmount = 449.85m, // 15 × 29.99
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            };
            _context.Orders.Add(pendingOrder);

            // Order 2: Approved status (20 units reserved)
            var approvedOrder = new Order
            {
                Id = ApprovedOrderId,
                Status = "approved",
                TotalAmount = 599.80m, // 20 × 29.99
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };
            _context.Orders.Add(approvedOrder);

            await _context.SaveChangesAsync();

            // Order items
            // NOTE: LocationId is critical for reserved quantity tracking!
            var orderItems = new List<OrderItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OrderId = PendingOrderId,
                    PartId = TestPartId,
                    LocationId = WarehouseId, // Reserve from Warehouse location
                    Quantity = 15,
                    UnitPrice = 29.99m
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    OrderId = ApprovedOrderId,
                    PartId = TestPartId,
                    LocationId = RetailId, // Reserve from Retail location
                    Quantity = 20,
                    UnitPrice = 29.99m
                }
            };

            _context.OrderItems.AddRange(orderItems);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestPurchaseOrdersAsync()
        {
            Console.WriteLine("  📋 Seeding test purchase orders (50 incoming units)...");

            // PurchaseOrder model properties: Id, VendorName, SupplierId, Status, OrderedAt, ExpectedDeliveryDate
            // PurchaseOrderItem model properties: Id, PurchaseOrderId, PartId, QuantityOrdered, QuantityReceived, UnitCost, LineAmount
            
            var po = new PurchaseOrder
            {
                Id = POId,
                SupplierId = SupplierId,
                Status = "approved",
                OrderDate = DateTime.UtcNow.AddDays(-5),
                ExpectedDeliveryDate = DateTime.UtcNow.AddDays(5),
                TotalAmount = 999.50m
            };

            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync();

            var poItem = new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = POId,
                PartId = TestPartId,
                QuantityOrdered = 50,
                QuantityReceived = 0, // Not yet received
                UnitCost = 19.99m,
                LineAmount = 999.50m // 50 × 19.99
            };

            _context.PurchaseOrderItems.Add(poItem);
            await _context.SaveChangesAsync();
        }

        private async Task VerifyDataAsync()
        {
            Console.WriteLine("\n  🔍 Verifying seeded data...");

            var totalOnHand = await _context.Inventory
                .Where(i => i.PartId == TestPartId)
                .SumAsync(i => i.QuantityOnHand);

            var reservedUnits = await (
                from oi in _context.OrderItems
                join o in _context.Orders on oi.OrderId equals o.Id
                where oi.PartId == TestPartId
                where o.Status == "pending" || o.Status == "approved"
                select oi.Quantity
            ).SumAsync();

            var incomingUnits = await (
                from poi in _context.PurchaseOrderItems
                join po in _context.PurchaseOrders on poi.PurchaseOrderId equals po.Id
                where poi.PartId == TestPartId
                where po.Status == "approved"
                select (poi.QuantityOrdered - poi.QuantityReceived)
            ).SumAsync();

            var effectiveAvailable = totalOnHand - reservedUnits + incomingUnits;

            Console.WriteLine($"\n  ✅ Verification Results:");
            Console.WriteLine($"     Total On Hand: {totalOnHand} units");
            Console.WriteLine($"     Reserved: {reservedUnits} units");
            Console.WriteLine($"     Incoming: {incomingUnits} units");
            Console.WriteLine($"     Effective Available: {effectiveAvailable} units");

            if (totalOnHand == 100 && reservedUnits == 35 && incomingUnits == 50 && effectiveAvailable == 115)
            {
                Console.WriteLine($"\n  🎉 All verification checks PASSED!");
            }
            else
            {
                Console.WriteLine($"\n  ⚠️  WARNING: Verification mismatch detected!");
            }
        }
    }
}
