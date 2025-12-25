using CloudWatcher.Data;
using CloudWatcher.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudWatcher.Seeds
{
    /// <summary>
    /// Database seeding service - populates initial reference data
    /// Run this after migrations are applied to set up default data
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly CloudWatcherContext _context;

        public DatabaseSeeder(CloudWatcherContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Seed all reference data
        /// </summary>
        public async Task SeedAllAsync()
        {
            try
            {
                await SeedRolesAsync();
                await SeedUsersAsync();
                await SeedDepartmentsAsync();
                await SeedUserRolesAsync();
                await SeedSuppliersAsync();
                await SeedPartsAsync();
                await SeedLocationsAsync();
                await SeedInventoryAsync();

                await _context.SaveChangesAsync();
                Console.WriteLine("✅ Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error seeding database: {ex.Message}");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            if (_context.Roles.Any())
            {
                Console.WriteLine("ℹ️ Roles already seeded, skipping...");
                return;
            }

            var roles = new List<Role>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "admin",
                    Description = "Full system access - can perform all operations"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "manager",
                    Description = "Department management - can manage users and orders"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "technician",
                    Description = "Device operations - can manage devices and requests"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "viewer",
                    Description = "Read-only access - can view data but not modify"
                }
            };

            await _context.Roles.AddRangeAsync(roles);
            Console.WriteLine($"✅ Seeded {roles.Count} roles");
        }

        private async Task SeedUsersAsync()
        {
            if (_context.Users.Any())
            {
                Console.WriteLine("ℹ️ Users already seeded, skipping...");
                return;
            }

            var users = new List<User>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Email = "admin@cloudwatcher.local",
                    Name = "System Administrator",
                    OAuthProvider = "local",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Email = "manager@cloudwatcher.local",
                    Name = "Department Manager",
                    OAuthProvider = "local",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Email = "technician@cloudwatcher.local",
                    Name = "Field Technician",
                    OAuthProvider = "local",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _context.Users.AddRangeAsync(users);
            Console.WriteLine($"✅ Seeded {users.Count} users");
        }

        private async Task SeedDepartmentsAsync()
        {
            if (_context.Departments.Any())
            {
                Console.WriteLine("ℹ️ Departments already seeded, skipping...");
                return;
            }

            var departments = new List<Department>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Operations",
                    Description = "Core operational department",
                    ParentId = null
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Fleet Management",
                    Description = "Vehicle and fleet operations",
                    ParentId = null
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Maintenance",
                    Description = "Parts and maintenance management",
                    ParentId = null
                }
            };

            await _context.Departments.AddRangeAsync(departments);
            Console.WriteLine($"✅ Seeded {departments.Count} departments");
        }

        private async Task SeedUserRolesAsync()
        {
            if (_context.UserRoles.Any())
            {
                Console.WriteLine("ℹ️ User roles already seeded, skipping...");
                return;
            }

            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "admin");
            var managerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "manager");
            var technicianRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "technician");

            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@cloudwatcher.local");
            var managerUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "manager@cloudwatcher.local");
            var techUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "technician@cloudwatcher.local");

            if (adminRole != null && adminUser != null)
            {
                _context.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });
            }

            if (managerRole != null && managerUser != null)
            {
                _context.UserRoles.Add(new UserRole { UserId = managerUser.Id, RoleId = managerRole.Id });
            }

            if (technicianRole != null && techUser != null)
            {
                _context.UserRoles.Add(new UserRole { UserId = techUser.Id, RoleId = technicianRole.Id });
            }

            Console.WriteLine("✅ Seeded user role assignments");
        }

        private async Task SeedSuppliersAsync()
        {
            if (_context.Suppliers.Any())
            {
                Console.WriteLine("ℹ️ Suppliers already seeded, skipping...");
                return;
            }

            var suppliers = new List<Supplier>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Standard Parts Inc",
                    ContactEmail = "sales@standardparts.com",
                    ContactPhone = "+1-800-555-0001",
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Premium Components Ltd",
                    ContactEmail = "orders@premiumcomp.com",
                    ContactPhone = "+1-800-555-0002",
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Fleet Supply Corp",
                    ContactEmail = "logistics@fleetsupply.com",
                    ContactPhone = "+1-800-555-0003",
                    IsActive = true
                }
            };

            await _context.Suppliers.AddRangeAsync(suppliers);
            Console.WriteLine($"✅ Seeded {suppliers.Count} suppliers");
        }

        private async Task SeedPartsAsync()
        {
            if (_context.Parts.Any())
            {
                Console.WriteLine("ℹ️ Parts already seeded, skipping...");
                return;
            }

            var parts = new List<Part>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Code = "P-BR-001",
                    Name = "Brake Pad Set",
                    Description = "Ceramic brake pads for front wheels",
                    Category = "Brakes",
                    StandardPrice = 49.99m
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Code = "P-OIL-001",
                    Name = "Synthetic Oil 5W-30",
                    Description = "Premium synthetic motor oil",
                    Category = "Fluids",
                    StandardPrice = 24.99m
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Code = "P-FLT-001",
                    Name = "Engine Air Filter",
                    Description = "High-flow air filter element",
                    Category = "Filters",
                    StandardPrice = 19.99m
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Code = "P-BAT-001",
                    Name = "12V Battery",
                    Description = "AGM automotive battery",
                    Category = "Electrical",
                    StandardPrice = 129.99m
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Code = "P-TIR-001",
                    Name = "All-Season Tire",
                    Description = "17-inch all-season radial tire",
                    Category = "Tires",
                    StandardPrice = 89.99m
                }
            };

            await _context.Parts.AddRangeAsync(parts);
            Console.WriteLine($"✅ Seeded {parts.Count} parts");
        }

        private async Task SeedLocationsAsync()
        {
            if (_context.Locations.Any())
            {
                Console.WriteLine("ℹ️ Locations already seeded, skipping...");
                return;
            }

            var locations = new List<Location>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Main Warehouse",
                    Address = "123 Industrial Blvd, Suite 100",
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Regional Depot",
                    Address = "456 Commerce Dr, Suite 200",
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Local Parts Store",
                    Address = "789 Retail Ave, Suite 300",
                    IsActive = true
                }
            };

            await _context.Locations.AddRangeAsync(locations);
            Console.WriteLine($"✅ Seeded {locations.Count} locations");
        }

        private async Task SeedInventoryAsync()
        {
            if (_context.Inventory.Any())
            {
                Console.WriteLine("ℹ️ Inventory already seeded, skipping...");
                return;
            }

            var parts = await _context.Parts.ToListAsync();
            var locations = await _context.Locations.ToListAsync();

            var inventory = new List<Inventory>();

            // Create inventory for each part at each location
            foreach (var part in parts)
            {
                foreach (var location in locations)
                {
                    inventory.Add(new Inventory
                    {
                        Id = Guid.NewGuid(),
                        PartId = part.Id,
                        LocationId = location.Id,
                        QuantityOnHand = Random.Shared.Next(10, 100),
                        ReorderLevel = 20,
                        ReorderQuantity = 50,
                        LastInventoryCheck = DateTime.UtcNow
                    });
                }
            }

            await _context.Inventory.AddRangeAsync(inventory);
            Console.WriteLine($"✅ Seeded {inventory.Count} inventory records");
        }
    }
}
