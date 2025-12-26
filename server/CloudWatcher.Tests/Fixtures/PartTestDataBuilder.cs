using CloudWatcher.Data;
using CloudWatcher.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudWatcher.Tests.Fixtures;

/// <summary>
/// Test data builder for Parts, PartVariants, Locations, and Inventory records.
/// Provides fluent API for creating consistent test data across integration tests.
/// </summary>
public class PartTestDataBuilder
{
    private readonly CloudWatcherContext _context;
    private Part? _currentPart;
    private readonly List<PartVariant> _variants = new();
    private readonly List<Location> _locations = new();
    private readonly List<Inventory> _inventories = new();

    public PartTestDataBuilder(CloudWatcherContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Create a new Part with default values.
    /// </summary>
    public PartTestDataBuilder CreatePart(string? code = null, string? name = null)
    {
        _currentPart = new Part
        {
            Id = Guid.NewGuid(),
            Code = code ?? $"PART-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
            Name = name ?? "Test Part",
            Description = "Test part description",
            Category = "Components",
            StandardPrice = 25.00m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        return this;
    }

    /// <summary>
    /// Add a PartVariant to the current Part.
    /// </summary>
    public PartTestDataBuilder AddVariant(string? variantCode = null, string? attributes = null)
    {
        if (_currentPart == null)
            throw new InvalidOperationException("Create a Part first using CreatePart()");

        var variant = new PartVariant
        {
            Id = Guid.NewGuid(),
            PartId = _currentPart.Id,
            VariantCode = variantCode ?? $"V-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
            Attributes = attributes ?? "{\"size\": \"standard\"}",
            VariantPrice = 10.50m,
            CreatedAt = DateTime.UtcNow
        };

        _variants.Add(variant);
        return this;
    }

    /// <summary>
    /// Add multiple variants to the current Part.
    /// </summary>
    public PartTestDataBuilder AddVariants(int count = 3)
    {
        if (_currentPart == null)
            throw new InvalidOperationException("Create a Part first using CreatePart()");

        for (int i = 0; i < count; i++)
        {
            AddVariant($"V-{i + 1}", $"{{\"variant\": \"variant-{i + 1}\"}}");
        }

        return this;
    }

    /// <summary>
    /// Add a Location to the test data.
    /// </summary>
    public PartTestDataBuilder AddLocation(string? name = null)
    {
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"Location-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
            DepartmentId = null,
            Address = "123 Warehouse Ave",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _locations.Add(location);
        return this;
    }

    /// <summary>
    /// Add multiple locations.
    /// </summary>
    public PartTestDataBuilder AddLocations(int count = 3)
    {
        for (int i = 0; i < count; i++)
        {
            AddLocation($"Location-{i + 1}");
        }

        return this;
    }

    /// <summary>
    /// Add inventory records for a specific variant and location combination.
    /// </summary>
    public PartTestDataBuilder AddInventory(PartVariant variant, Location location, int quantity, int reorderLevel = 10)
    {
        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            PartId = _currentPart?.Id ?? variant.PartId,
            LocationId = location.Id,
            QuantityOnHand = quantity,
            ReorderLevel = reorderLevel,
            ReorderQuantity = reorderLevel * 2,
            LastInventoryCheck = DateTime.UtcNow.AddDays(-1)
        };

        _inventories.Add(inventory);
        return this;
    }

    /// <summary>
    /// Populate inventory for all variants and locations.
    /// </summary>
    public PartTestDataBuilder PopulateAllInventory(int quantity = 100, int reorderLevel = 10)
    {
        if (!_variants.Any() || !_locations.Any())
            throw new InvalidOperationException("Add variants and locations before populating inventory");

        foreach (var variant in _variants)
        {
            foreach (var location in _locations)
            {
                // Check if already has inventory
                if (!_inventories.Any(i => i.PartId == variant.PartId && i.LocationId == location.Id))
                {
                    AddInventory(variant, location, quantity, reorderLevel);
                }
            }
        }

        return this;
    }

    /// <summary>
    /// Build and save all test data to the context.
    /// </summary>
    public Part Build()
    {
        if (_currentPart == null)
            throw new InvalidOperationException("No Part created. Call CreatePart() first.");

        // Add all entities to context
        _context.Parts.Add(_currentPart);

        foreach (var variant in _variants)
        {
            _context.PartVariants.Add(variant);
        }

        foreach (var location in _locations)
        {
            _context.Locations.Add(location);
        }

        foreach (var inventory in _inventories)
        {
            _context.Inventory.Add(inventory);
        }

        _context.SaveChanges();

        return _currentPart;
    }

    /// <summary>
    /// Get a copy of the current Part with all related data loaded.
    /// </summary>
    public Part? GetPartWithDetails()
    {
        if (_currentPart == null)
            throw new InvalidOperationException("No Part created. Call CreatePart() first.");

        return _context.Parts
            .Where(p => p.Id == _currentPart.Id)
            .Include(p => p.Variants)
            .Include(p => p.InventoryRecords)
                .ThenInclude(i => i.Location)
            .FirstOrDefault();
    }

    /// <summary>
    /// Get the current Part ID for API testing.
    /// </summary>
    public Guid GetPartId()
    {
        if (_currentPart == null)
            throw new InvalidOperationException("No Part created. Call CreatePart() first.");

        return _currentPart.Id;
    }

    /// <summary>
    /// Fluent preset: Create a simple part with 2 variants and 2 locations.
    /// </summary>
    public PartTestDataBuilder SimpleSetup()
    {
        CreatePart("SIMPLE-001", "Simple Test Part")
            .AddVariants(2)
            .AddLocations(2)
            .PopulateAllInventory(50, 10);

        return this;
    }

    /// <summary>
    /// Fluent preset: Create a complex part with 5 variants, 4 locations.
    /// </summary>
    public PartTestDataBuilder ComplexSetup()
    {
        CreatePart("COMPLEX-001", "Complex Test Part")
            .AddVariants(5)
            .AddLocations(4)
            .PopulateAllInventory(100, 15);

        return this;
    }

    /// <summary>
    /// Fluent preset: Create a part that tests edge cases (single variant, single location).
    /// </summary>
    public PartTestDataBuilder MinimalSetup()
    {
        CreatePart("MINIMAL-001", "Minimal Test Part")
            .AddVariants(1)
            .AddLocations(1)
            .PopulateAllInventory(5, 2);

        return this;
    }

    /// <summary>
    /// Fluent preset: Create a part with low stock at one location.
    /// </summary>
    public PartTestDataBuilder LowStockSetup()
    {
        CreatePart("LOWSTOCK-001", "Low Stock Test Part")
            .AddVariants(2)
            .AddLocations(2);

        // Add normal stock at first location
        if (_variants.Any() && _locations.Any())
        {
            AddInventory(_variants[0], _locations[0], 100, 10);
            // Add low stock at second location
            AddInventory(_variants[0], _locations[1], 2, 10);
        }

        return this;
    }
}

/// <summary>
/// Static helper methods for common test data scenarios.
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// Create a Part with specified structure for testing.
    /// </summary>
    public static Part CreateTestPart(
        CloudWatcherContext context,
        string? code = null,
        string? name = null,
        int variantCount = 2,
        int locationCount = 2)
    {
        return new PartTestDataBuilder(context)
            .CreatePart(code, name)
            .AddVariants(variantCount)
            .AddLocations(locationCount)
            .PopulateAllInventory(100, 15)
            .Build();
    }

    /// <summary>
    /// Create multiple parts for bulk testing.
    /// </summary>
    public static List<Part> CreateTestParts(CloudWatcherContext context, int count = 5)
    {
        var parts = new List<Part>();
        for (int i = 0; i < count; i++)
        {
            parts.Add(new PartTestDataBuilder(context)
                .CreatePart($"BULK-{i + 1:D3}", $"Bulk Test Part {i + 1}")
                .AddVariants(2)
                .AddLocations(2)
                .PopulateAllInventory(75, 12)
                .Build());
        }
        return parts;
    }

    /// <summary>
    /// Create a part that triggers low stock indicators.
    /// </summary>
    public static Part CreateLowStockPart(CloudWatcherContext context)
    {
        return new PartTestDataBuilder(context)
            .LowStockSetup()
            .Build();
    }

    /// <summary>
    /// Create a simple part for basic endpoint testing.
    /// </summary>
    public static Part CreateSimplePart(CloudWatcherContext context)
    {
        return new PartTestDataBuilder(context)
            .SimpleSetup()
            .Build();
    }

    /// <summary>
    /// Create a complex part with many variants and locations.
    /// </summary>
    public static Part CreateComplexPart(CloudWatcherContext context)
    {
        return new PartTestDataBuilder(context)
            .ComplexSetup()
            .Build();
    }
}

