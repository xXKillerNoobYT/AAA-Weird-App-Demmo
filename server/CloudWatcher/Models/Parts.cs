using System;
using System.Collections.Generic;

namespace CloudWatcher.Models
{
    /// <summary>
    /// Part entity - represents vehicle parts or components
    /// </summary>
    public class Part
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = null!; // Unique part code
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public decimal StandardPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// PartVariant - represents variations of a part (size, color, material, etc.)
    /// </summary>
    public class PartVariant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PartId { get; set; }
        public string VariantCode { get; set; } = null!;
        public string? Attributes { get; set; } // JSON: {size: 'L', color: 'red'}
        public decimal? VariantPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Supplier entity - represents vendors/suppliers for parts
    /// </summary>
    public class Supplier
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// PartSupplier junction table - maps parts to suppliers with pricing
    /// </summary>
    public class PartSupplier
    {
        public Guid PartId { get; set; }
        public Guid SupplierId { get; set; }
        public string SKU { get; set; } = null!;
        public decimal SupplierPrice { get; set; }
        public int LeadTimeDays { get; set; }
        public int MinimumOrderQuantity { get; set; } = 1;
    }

    /// <summary>
    /// Location entity - represents storage locations for inventory
    /// </summary>
    public class Location
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public Guid? DepartmentId { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Inventory entity - represents physical inventory of parts at locations
    /// </summary>
    public class Inventory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PartId { get; set; }
        public Guid LocationId { get; set; }
        public int QuantityOnHand { get; set; }
        public int ReorderLevel { get; set; }
        public int ReorderQuantity { get; set; }
        public DateTime LastInventoryCheck { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// StockLevel entity - tracks current stock levels (denormalized for performance)
    /// </summary>
    public class StockLevel
    {
        public Guid PartId { get; set; }
        public Guid LocationId { get; set; }
        public int Quantity { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// PartConsumable - tracks consumable parts usage at locations
    /// </summary>
    public class PartConsumable
    {
        public Guid PartId { get; set; }
        public Guid LocationId { get; set; }
        public int QuantityConsumed { get; set; }
        public DateTime LastConsumedAt { get; set; } = DateTime.UtcNow;
    }
}
