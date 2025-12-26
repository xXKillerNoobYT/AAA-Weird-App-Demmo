using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CloudWatcher.Controllers;
using CloudWatcher.Data;
using CloudWatcher.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace CloudWatcher.Tests.Integration
{
    /// <summary>
    /// Integration tests for InventoryControllerV2 - Wave 4.2
    /// Tests GET /api/v2/inventory/{partId} endpoint with database interactions
    /// </summary>
    public class InventoryControllerV2Tests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public InventoryControllerV2Tests(TestWebApplicationFactory factory, ITestOutputHelper output) 
            : base(factory)
        {
            _output = output;
        }

        /// <summary>
        /// Test: GET /api/v2/inventory/{partId} with valid part ID returns 200 and part details
        /// </summary>
        [Fact]
        public async Task GetPartById_ValidPartId_Returns200WithDetails()
        {
            // Arrange - set authorization
            SetAuthorizationToken();

            // Arrange - seed database with test data
            var partId = Guid.NewGuid();
            var locationId1 = Guid.NewGuid();
            var locationId2 = Guid.NewGuid();
            var variantId1 = Guid.NewGuid();
            var variantId2 = Guid.NewGuid();

            using (var scope = Factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();
                
                // Create test part
                var part = new Part
                {
                    Id = partId,
                    Code = "TEST-001",
                    Name = "Test Part",
                    Description = "Integration test part",
                    Category = "TestCategory",
                    StandardPrice = 99.99m,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Parts.Add(part);

                // Create variants
                var variant1 = new PartVariant
                {
                    Id = variantId1,
                    PartId = partId,
                    VariantCode = "VAR-001",
                    Attributes = "{\"color\":\"red\",\"size\":\"large\"}",
                    VariantPrice = 109.99m,
                    CreatedAt = DateTime.UtcNow
                };
                var variant2 = new PartVariant
                {
                    Id = variantId2,
                    PartId = partId,
                    VariantCode = "VAR-002",
                    Attributes = "{\"color\":\"blue\",\"size\":\"medium\"}",
                    VariantPrice = 89.99m,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.PartVariants.Add(variant1);
                dbContext.PartVariants.Add(variant2);

                // Create locations
                var location1 = new Location
                {
                    Id = locationId1,
                    Name = "Warehouse A",
                    Address = "123 Test St",
                    CreatedAt = DateTime.UtcNow
                };
                var location2 = new Location
                {
                    Id = locationId2,
                    Name = "Warehouse B",
                    Address = "456 Test Ave",
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Locations.Add(location1);
                dbContext.Locations.Add(location2);

                // Create inventory records
                var inventory1 = new Inventory
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    LocationId = locationId1,
                    QuantityOnHand = 50,
                    ReorderLevel = 20,
                    LastInventoryCheck = DateTime.UtcNow
                };
                var inventory2 = new Inventory
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    LocationId = locationId2,
                    QuantityOnHand = 15,
                    ReorderLevel = 10,
                    LastInventoryCheck = DateTime.UtcNow
                };
                dbContext.Inventory.Add(inventory1);
                dbContext.Inventory.Add(inventory2);

                await dbContext.SaveChangesAsync();
            }

            // Act - call endpoint
            var response = await Client.GetAsync($"/api/v2/inventory/{partId}");

            // Assert - verify response
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<InventoryControllerV2.PartDetailResponse>();
            Assert.NotNull(result);
            Assert.Equal(partId.ToString(), result.PartId);
            Assert.Equal("TEST-001", result.PartCode);
            Assert.Equal("Test Part", result.PartName);
            Assert.Equal("Integration test part", result.Description);
            Assert.Equal("TestCategory", result.Category);
            Assert.Equal(99.99m, result.StandardPrice);
            Assert.Equal(65, result.TotalQuantityOnHand); // 50 + 15

            // Verify variants
            Assert.Equal(2, result.Variants.Count);
            var variant1Result = result.Variants.Find(v => v.VariantCode == "VAR-001");
            Assert.NotNull(variant1Result);
            Assert.Equal(109.99m, variant1Result.VariantPrice);
            Assert.Contains("red", variant1Result.Attributes);

            // Verify locations
            Assert.Equal(2, result.Locations.Count);
            var loc1 = result.Locations.Find(l => l.LocationName == "Warehouse A");
            Assert.NotNull(loc1);
            Assert.Equal(50, loc1.QuantityOnHand);
            Assert.Equal(20, loc1.ReorderLevel);
            Assert.False(loc1.IsLowStock); // 50 >= 20

            var loc2 = result.Locations.Find(l => l.LocationName == "Warehouse B");
            Assert.NotNull(loc2);
            Assert.Equal(15, loc2.QuantityOnHand);
            Assert.Equal(10, loc2.ReorderLevel);
            Assert.False(loc2.IsLowStock); // 15 >= 10

            _output.WriteLine($"✅ GET /api/v2/inventory/{partId} returned correct part details with 2 variants and 2 locations");
        }

        /// <summary>
        /// Test: GET /api/v2/inventory/{partId} with invalid UUID returns 400
        /// </summary>
        [Fact]
        public async Task GetPartById_InvalidUUID_Returns400()
        {
            // Arrange - set authorization
            SetAuthorizationToken();

            // Act - call with invalid UUID
            var response = await Client.GetAsync("/api/v2/inventory/not-a-valid-uuid");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var errorJson = await response.Content.ReadAsStringAsync();
            Assert.Contains("valid UUID", errorJson);

            _output.WriteLine("✅ GET /api/v2/inventory/not-a-valid-uuid returned 400 BadRequest as expected");
        }

        /// <summary>
        /// Test: GET /api/v2/inventory/{partId} with missing part returns 404
        /// </summary>
        [Fact]
        public async Task GetPartById_MissingPart_Returns404()
        {
            // Arrange - set authorization
            SetAuthorizationToken();

            // Arrange - use a valid UUID that doesn't exist
            var nonExistentPartId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync($"/api/v2/inventory/{nonExistentPartId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var errorJson = await response.Content.ReadAsStringAsync();
            Assert.Contains("Part not found", errorJson);

            _output.WriteLine($"✅ GET /api/v2/inventory/{nonExistentPartId} returned 404 NotFound as expected");
        }

        /// <summary>
        /// Test: GET /api/v2/inventory/{partId}?includeArchived=false excludes archived variants
        /// </summary>
        [Fact]
        public async Task GetPartById_WithoutIncludeArchived_ExcludesArchivedVariants()
        {
            // Arrange - set authorization
            SetAuthorizationToken();

            // Arrange - seed database with archived variant
            var partId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            using (var scope = Factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();

                // Create part
                var part = new Part
                {
                    Id = partId,
                    Code = "TEST-ARCHIVE-001",
                    Name = "Part with Archived Variants",
                    Category = "Test",
                    StandardPrice = 50.00m,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Parts.Add(part);

                // Create active variant
                var activeVariant = new PartVariant
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    VariantCode = "ACTIVE-001",
                    Attributes = "{}",
                    VariantPrice = 55.00m,
                    CreatedAt = DateTime.UtcNow
                };

                // Create archived variant (starts with "ARCHIVED_")
                var archivedVariant = new PartVariant
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    VariantCode = "ARCHIVED_OLD-001",
                    Attributes = "{}",
                    VariantPrice = 45.00m,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                };

                dbContext.PartVariants.Add(activeVariant);
                dbContext.PartVariants.Add(archivedVariant);

                // Create location and inventory
                var location = new Location
                {
                    Id = locationId,
                    Name = "Main Warehouse",
                    Address = "789 Archive St",
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Locations.Add(location);

                var inventory = new Inventory
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    LocationId = locationId,
                    QuantityOnHand = 100,
                    ReorderLevel = 25,
                    LastInventoryCheck = DateTime.UtcNow
                };
                dbContext.Inventory.Add(inventory);

                await dbContext.SaveChangesAsync();
            }

            // Act - call without includeArchived (defaults to false)
            var response = await Client.GetAsync($"/api/v2/inventory/{partId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<InventoryControllerV2.PartDetailResponse>();
            Assert.NotNull(result);
            Assert.Equal(1, result.Variants.Count); // Only active variant
            Assert.Equal("ACTIVE-001", result.Variants[0].VariantCode);

            _output.WriteLine($"✅ GET /api/v2/inventory/{partId} excluded archived variants (returned 1 active variant)");
        }

        /// <summary>
        /// Test: GET /api/v2/inventory/{partId}?includeArchived=true includes archived variants
        /// </summary>
        [Fact]
        public async Task GetPartById_WithIncludeArchived_IncludesArchivedVariants()
        {
            // Arrange - set authorization
            SetAuthorizationToken();

            // Arrange - seed database (reuse setup from previous test)
            var partId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            using (var scope = Factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();

                var part = new Part
                {
                    Id = partId,
                    Code = "TEST-ARCHIVE-002",
                    Name = "Part for includeArchived Test",
                    Category = "Test",
                    StandardPrice = 60.00m,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Parts.Add(part);

                var activeVariant = new PartVariant
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    VariantCode = "ACTIVE-002",
                    Attributes = "{}",
                    VariantPrice = 65.00m,
                    CreatedAt = DateTime.UtcNow
                };

                var archivedVariant = new PartVariant
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    VariantCode = "ARCHIVED_OLD-002",
                    Attributes = "{}",
                    VariantPrice = 50.00m,
                    CreatedAt = DateTime.UtcNow.AddMonths(-12)
                };

                dbContext.PartVariants.Add(activeVariant);
                dbContext.PartVariants.Add(archivedVariant);

                var location = new Location
                {
                    Id = locationId,
                    Name = "Archive Test Warehouse",
                    Address = "321 Archive Ave",
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Locations.Add(location);

                var inventory = new Inventory
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    LocationId = locationId,
                    QuantityOnHand = 200,
                    ReorderLevel = 50,
                    LastInventoryCheck = DateTime.UtcNow
                };
                dbContext.Inventory.Add(inventory);

                await dbContext.SaveChangesAsync();
            }

            // Act - call WITH includeArchived=true
            var response = await Client.GetAsync($"/api/v2/inventory/{partId}?includeArchived=true");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<InventoryControllerV2.PartDetailResponse>();
            Assert.NotNull(result);
            Assert.Equal(2, result.Variants.Count); // Both active and archived variants
            
            var activeVar = result.Variants.Find(v => v.VariantCode == "ACTIVE-002");
            var archivedVar = result.Variants.Find(v => v.VariantCode == "ARCHIVED_OLD-002");

            Assert.NotNull(activeVar);
            Assert.NotNull(archivedVar);

            _output.WriteLine($"✅ GET /api/v2/inventory/{partId}?includeArchived=true returned 2 variants (active + archived)");
        }

        /// <summary>
        /// Test: GET /api/v2/inventory/{partId} with part that has low stock location
        /// </summary>
        [Fact]
        public async Task GetPartById_WithLowStockLocation_IndicatesLowStock()
        {
            // Arrange - set authorization
            SetAuthorizationToken();

            // Arrange - seed with low stock inventory
            var partId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            using (var scope = Factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();

                var part = new Part
                {
                    Id = partId,
                    Code = "TEST-LOWSTOCK-001",
                    Name = "Low Stock Test Part",
                    Category = "Test",
                    StandardPrice = 25.00m,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Parts.Add(part);

                var variant = new PartVariant
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    VariantCode = "VAR-LOW-001",
                    Attributes = "{}",
                    VariantPrice = 30.00m,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.PartVariants.Add(variant);

                var lowStockLocation = new Location
                {
                    Id = locationId,
                    Name = "Low Stock Warehouse",
                    Address = "999 Empty St",
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Locations.Add(lowStockLocation);

                // Inventory with quantity BELOW reorder level
                var inventory = new Inventory
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    LocationId = locationId,
                    QuantityOnHand = 5,       // Below reorder level
                    ReorderLevel = 20,        // Reorder threshold
                    LastInventoryCheck = DateTime.UtcNow
                };
                dbContext.Inventory.Add(inventory);

                await dbContext.SaveChangesAsync();
            }

            // Act
            var response = await Client.GetAsync($"/api/v2/inventory/{partId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<InventoryControllerV2.PartDetailResponse>();
            Assert.NotNull(result);
            Assert.Single(result.Locations);

            var location = result.Locations[0];
            Assert.Equal(5, location.QuantityOnHand);
            Assert.Equal(20, location.ReorderLevel);
            Assert.True(location.IsLowStock); // 5 < 20, should be flagged as low stock

            _output.WriteLine($"✅ GET /api/v2/inventory/{partId} correctly identified low stock (5 < 20 reorder level)");
        }
    }
}
