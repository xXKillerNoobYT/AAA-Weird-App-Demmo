using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudWatcher.Controllers;
using CloudWatcher.Data;
using CloudWatcher.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CloudWatcher.Tests.Controllers;

/// <summary>
/// Unit tests for InventoryControllerV2.GetPartById endpoint
/// Tests controller logic including UUID validation, part lookup, inventory aggregation
/// </summary>
public class InventoryControllerV2GetPartByIdTests
{
    private CloudWatcherContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<CloudWatcherContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new CloudWatcherContext(options);
    }

    private Mock<ILogger<InventoryControllerV2>> CreateMockLogger()
    {
        return new Mock<ILogger<InventoryControllerV2>>();
    }

    [Fact]
    public async Task GetPartById_WithValidUuid_ReturnsOkWithPartDetails()
    {
        // Arrange
        var context = CreateTestContext();
        var logger = CreateMockLogger();
        var controller = new InventoryControllerV2(context, logger.Object);

        var partId = Guid.NewGuid();
        var part = new Part
        {
            Id = partId,
            Code = "PART-001",
            Name = "Test Widget",
            Description = "A test widget",
            Category = "Components",
            StandardPrice = 99.99m,
            CreatedAt = DateTime.UtcNow
        };

        context.Parts.Add(part);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetPartById(partId.ToString(), includeArchived: false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
        var response = Assert.IsType<InventoryControllerV2.PartDetailResponse>(okResult.Value);
        Assert.Equal(partId.ToString(), response.PartId);
        Assert.Equal("PART-001", response.PartCode);
        Assert.Equal("Test Widget", response.PartName);
    }

    [Fact]
    public async Task GetPartById_WithInvalidUuidFormat_ReturnsBadRequest()
    {
        // Arrange
        var context = CreateTestContext();
        var logger = CreateMockLogger();
        var controller = new InventoryControllerV2(context, logger.Object);

        var invalidUuid = "not-a-valid-uuid";

        // Act
        var result = await controller.GetPartById(invalidUuid, includeArchived: false);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task GetPartById_WithNonExistentPart_ReturnsNotFound()
    {
        // Arrange
        var context = CreateTestContext();
        var logger = CreateMockLogger();
        var controller = new InventoryControllerV2(context, logger.Object);

        var nonExistentPartId = Guid.NewGuid();

        // Act
        var result = await controller.GetPartById(nonExistentPartId.ToString(), includeArchived: false);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetPartById_WithVariants_IncludesVariantsInResponse()
    {
        // Arrange
        var context = CreateTestContext();
        var logger = CreateMockLogger();
        var controller = new InventoryControllerV2(context, logger.Object);

        var partId = Guid.NewGuid();
        var part = new Part
        {
            Id = partId,
            Code = "PART-VAR",
            Name = "Part With Variants",
            StandardPrice = 100m,
            CreatedAt = DateTime.UtcNow,
            Variants = new List<PartVariant>
            {
                new PartVariant
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    VariantCode = "VAR-001",
                    Attributes = "{\"size\": \"L\", \"color\": \"red\"}",
                    VariantPrice = 110m,
                    CreatedAt = DateTime.UtcNow
                },
                new PartVariant
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    VariantCode = "VAR-002",
                    Attributes = "{\"size\": \"M\", \"color\": \"blue\"}",
                    VariantPrice = 105m,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        context.Parts.Add(part);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetPartById(partId.ToString(), includeArchived: false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InventoryControllerV2.PartDetailResponse>(okResult.Value);
        Assert.NotNull(response.Variants);
        Assert.Equal(2, response.Variants.Count);
        Assert.Contains(response.Variants, v => v.VariantCode == "VAR-001");
        Assert.Contains(response.Variants, v => v.VariantCode == "VAR-002");
    }

    [Fact]
    public async Task GetPartById_WithArchivedVariants_ExcludesWhenIncludeArchivedFalse()
    {
        // Arrange
        var context = CreateTestContext();
        var logger = CreateMockLogger();
        var controller = new InventoryControllerV2(context, logger.Object);

        var partId = Guid.NewGuid();
        var part = new Part
        {
            Id = partId,
            Code = "PART-ARCH",
            Name = "Part With Archived Variants",
            StandardPrice = 100m,
            CreatedAt = DateTime.UtcNow,
            Variants = new List<PartVariant>
            {
                new PartVariant
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    VariantCode = "VAR-ACTIVE",
                    CreatedAt = DateTime.UtcNow
                },
                new PartVariant
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    VariantCode = "ARCHIVED_VAR-OLD",
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        context.Parts.Add(part);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetPartById(partId.ToString(), includeArchived: false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InventoryControllerV2.PartDetailResponse>(okResult.Value);
        Assert.Single(response.Variants);
        Assert.Equal("VAR-ACTIVE", response.Variants[0].VariantCode);
    }

    [Fact]
    public async Task GetPartById_WithIncludeArchivedTrue_IncludesArchivedVariants()
    {
        // Arrange
        var context = CreateTestContext();
        var logger = CreateMockLogger();
        var controller = new InventoryControllerV2(context, logger.Object);

        var partId = Guid.NewGuid();
        var part = new Part
        {
            Id = partId,
            Code = "PART-ARCH2",
            Name = "Part With Archived Variants",
            StandardPrice = 100m,
            CreatedAt = DateTime.UtcNow,
            Variants = new List<PartVariant>
            {
                new PartVariant
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    VariantCode = "VAR-ACTIVE",
                    CreatedAt = DateTime.UtcNow
                },
                new PartVariant
                {
                    Id = Guid.NewGuid(),
                    PartId = partId,
                    VariantCode = "ARCHIVED_VAR-OLD",
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        context.Parts.Add(part);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetPartById(partId.ToString(), includeArchived: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InventoryControllerV2.PartDetailResponse>(okResult.Value);
        Assert.Equal(2, response.Variants.Count);
    }

    [Fact]
    public async Task GetPartById_WithInventoryRecords_IncludesLocationBreakdown()
    {
        // Arrange
        var context = CreateTestContext();
        var logger = CreateMockLogger();
        var controller = new InventoryControllerV2(context, logger.Object);

        var partId = Guid.NewGuid();
        var location1Id = Guid.NewGuid();
        var location2Id = Guid.NewGuid();

        var part = new Part
        {
            Id = partId,
            Code = "PART-INV",
            Name = "Part With Inventory",
            StandardPrice = 100m,
            CreatedAt = DateTime.UtcNow
        };

        var location1 = new Location
        {
            Id = location1Id,
            Name = "Warehouse A",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var location2 = new Location
        {
            Id = location2Id,
            Name = "Warehouse B",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var inventory1 = new Inventory
        {
            Id = Guid.NewGuid(),
            PartId = partId,
            LocationId = location1Id,
            QuantityOnHand = 100,
            ReorderLevel = 50,
            ReorderQuantity = 100,
            LastInventoryCheck = DateTime.UtcNow
        };

        var inventory2 = new Inventory
        {
            Id = Guid.NewGuid(),
            PartId = partId,
            LocationId = location2Id,
            QuantityOnHand = 50,
            ReorderLevel = 50,
            ReorderQuantity = 100,
            LastInventoryCheck = DateTime.UtcNow
        };

        context.Parts.Add(part);
        context.Locations.Add(location1);
        context.Locations.Add(location2);
        context.Inventory.Add(inventory1);
        context.Inventory.Add(inventory2);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetPartById(partId.ToString(), includeArchived: false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InventoryControllerV2.PartDetailResponse>(okResult.Value);
        Assert.NotNull(response.Locations);
        Assert.Equal(2, response.Locations.Count);
        Assert.Equal(150, response.TotalQuantityOnHand);
    }

    [Fact]
    public async Task GetPartById_CalculatesTotalQuantityCorrectly()
    {
        // Arrange
        var context = CreateTestContext();
        var logger = CreateMockLogger();
        var controller = new InventoryControllerV2(context, logger.Object);

        var partId = Guid.NewGuid();
        var part = new Part
        {
            Id = partId,
            Code = "PART-QTY",
            Name = "Quantity Test Part",
            StandardPrice = 100m,
            CreatedAt = DateTime.UtcNow
        };

        context.Parts.Add(part);

        // Add multiple inventory records with different quantities
        for (int i = 0; i < 3; i++)
        {
            var locationId = Guid.NewGuid();
            var location = new Location
            {
                Id = locationId,
                Name = $"Location {i}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Locations.Add(location);

            var inventory = new Inventory
            {
                Id = Guid.NewGuid(),
                PartId = partId,
                LocationId = locationId,
                QuantityOnHand = (i + 1) * 10,
                ReorderLevel = 10,
                ReorderQuantity = 20,
                LastInventoryCheck = DateTime.UtcNow
            };
            context.Inventory.Add(inventory);
        }

        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetPartById(partId.ToString(), includeArchived: false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InventoryControllerV2.PartDetailResponse>(okResult.Value);
        Assert.Equal(60, response.TotalQuantityOnHand); // 10 + 20 + 30
    }

    [Fact]
    public async Task GetPartById_WithLowStockLocations_MarksFlagCorrectly()
    {
        // Arrange
        var context = CreateTestContext();
        var logger = CreateMockLogger();
        var controller = new InventoryControllerV2(context, logger.Object);

        var partId = Guid.NewGuid();
        var part = new Part
        {
            Id = partId,
            Code = "PART-LOW",
            Name = "Low Stock Part",
            StandardPrice = 100m,
            CreatedAt = DateTime.UtcNow
        };

        var locationId = Guid.NewGuid();
        var location = new Location
        {
            Id = locationId,
            Name = "Low Stock Warehouse",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            PartId = partId,
            LocationId = locationId,
            QuantityOnHand = 5,  // Below reorder level
            ReorderLevel = 50,
            ReorderQuantity = 100,
            LastInventoryCheck = DateTime.UtcNow
        };

        context.Parts.Add(part);
        context.Locations.Add(location);
        context.Inventory.Add(inventory);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetPartById(partId.ToString(), includeArchived: false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InventoryControllerV2.PartDetailResponse>(okResult.Value);
        Assert.Single(response.Locations);
        Assert.True(response.Locations[0].IsLowStock);
    }

    [Fact]
    public async Task GetPartById_WithPartWithoutInventory_ReturnsEmptyLocationsList()
    {
        // Arrange
        var context = CreateTestContext();
        var logger = CreateMockLogger();
        var controller = new InventoryControllerV2(context, logger.Object);

        var partId = Guid.NewGuid();
        var part = new Part
        {
            Id = partId,
            Code = "PART-NO-INV",
            Name = "Part Without Inventory",
            StandardPrice = 100m,
            CreatedAt = DateTime.UtcNow
        };

        context.Parts.Add(part);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetPartById(partId.ToString(), includeArchived: false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InventoryControllerV2.PartDetailResponse>(okResult.Value);
        Assert.Empty(response.Locations);
        Assert.Equal(0, response.TotalQuantityOnHand);
    }

    [Fact]
    public async Task GetPartById_ResponseIncludesMetadata()
    {
        // Arrange
        var context = CreateTestContext();
        var logger = CreateMockLogger();
        var controller = new InventoryControllerV2(context, logger.Object);

        var partId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddHours(-1);
        var part = new Part
        {
            Id = partId,
            Code = "PART-META",
            Name = "Metadata Test Part",
            Description = "Test Description",
            Category = "Test Category",
            StandardPrice = 123.45m,
            CreatedAt = createdAt,
            UpdatedAt = DateTime.UtcNow
        };

        context.Parts.Add(part);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetPartById(partId.ToString(), includeArchived: false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InventoryControllerV2.PartDetailResponse>(okResult.Value);
        Assert.Equal("Test Description", response.Description);
        Assert.Equal("Test Category", response.Category);
        Assert.Equal(123.45m, response.StandardPrice);
        Assert.NotNull(response.CreatedAt);
        Assert.NotNull(response.UpdatedAt);
    }
}
