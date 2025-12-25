using System;
using System.Collections.Generic;

namespace CloudWatcher.Models
{
    /// <summary>
    /// Order entity - represents part orders created from requests
    /// </summary>
    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? RequestId { get; set; }
        public string Status { get; set; } = "pending"; // pending, approved, shipped, delivered, cancelled
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        // Navigation properties for reserved quantity tracking
        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public virtual Request? Request { get; set; }
    }

    /// <summary>
    /// OrderItem - represents line items in an order
    /// </summary>
    public class OrderItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public Guid PartId { get; set; }
        public Guid? LocationId { get; set; } // FK to Location - for reserved quantity tracking
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineAmount { get; set; }

        // Navigation properties for reserved quantity tracking
        public virtual Order? Order { get; set; }
        public virtual Part? Part { get; set; }
        public virtual Location? Location { get; set; }
    }

    /// <summary>
    /// OrderApproval - tracks order approvals and approval chain
    /// </summary>
    public class OrderApproval
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public Guid ApproverId { get; set; } // FK to User
        public string Status { get; set; } = "pending"; // pending, approved, rejected
        public string? Notes { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
    }

    /// <summary>
    /// OrderHistory - audit trail for order status changes
    /// </summary>
    public class OrderHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public string Event { get; set; } = null!; // 'created', 'approved', 'shipped', 'delivered'
        public Guid? UserId { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// PurchaseOrder - represents purchase orders from suppliers
    /// </summary>
    public class PurchaseOrder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SupplierId { get; set; }
        public string Status { get; set; } = "pending"; // pending, approved, received, cancelled
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public bool IsFullyReceived { get; set; } = false;

        // Navigation properties for incoming inventory tracking
        public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
        public virtual Supplier? Supplier { get; set; }
    }

    /// <summary>
    /// PurchaseOrderItem - represents line items in a purchase order
    /// </summary>
    public class PurchaseOrderItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PurchaseOrderId { get; set; }
        public Guid PartId { get; set; }
        public int QuantityOrdered { get; set; }
        public int QuantityReceived { get; set; } = 0;
        public decimal UnitCost { get; set; }
        public decimal LineAmount { get; set; }

        // Navigation properties for incoming inventory tracking
        public virtual PurchaseOrder? PurchaseOrder { get; set; }
        public virtual Part? Part { get; set; }
    }
}
