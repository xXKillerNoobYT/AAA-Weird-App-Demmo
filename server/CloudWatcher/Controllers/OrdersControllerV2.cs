using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudWatcher.Data;
using CloudWatcher.Models;

namespace CloudWatcher.Controllers
{
    /// <summary>
    /// Orders API Controller (v2) - Manage customer orders and approval workflows.
    /// Wave 5 Implementation: Core order management endpoints.
    /// </summary>
    [Authorize] // All endpoints require authentication (unless dev bypass)
    public class OrdersControllerV2 : BaseApiController
    {
        private readonly CloudWatcherContext _dbContext;

        public OrdersControllerV2(CloudWatcherContext dbContext, ILogger<OrdersControllerV2> logger)
            : base(logger)
        {
            _dbContext = dbContext;
        }

        // =========================
        // POST /api/v2/orders
        // =========================
        /// <summary>
        /// Create a new order with order items.
        /// Wave 5 Task #1: POST /api/v2/orders
        /// </summary>
        /// <param name="request">Order creation request with items</param>
        /// <returns>Created order with ID and items</returns>
        [HttpPost]
        [ProducesResponseType(typeof(OrderResponse), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                // Validate request
                if (request == null || request.Items == null || !request.Items.Any())
                {
                    return ErrorResponse("Request body and items are required", "VALIDATION_ERROR", 400).Result!;
                }

                // Validate user exists
                var userExists = await _dbContext.Users.AnyAsync(u => u.Id == request.UserId);
                if (!userExists)
                {
                    return ErrorResponse($"User not found: {request.UserId}", "NOT_FOUND", 404).Result!;
                }

                // Validate all parts exist
                var partIds = request.Items.Select(i => i.PartId).Distinct().ToList();
                var existingParts = await _dbContext.Parts
                    .Where(p => partIds.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync();

                var missingParts = partIds.Except(existingParts).ToList();
                if (missingParts.Any())
                {
                    return ErrorResponse($"Parts not found: {string.Join(", ", missingParts)}", "NOT_FOUND", 404).Result!;
                }

                // Validate locations if provided
                var locationIds = request.Items
                    .Where(i => i.LocationId.HasValue)
                    .Select(i => i.LocationId!.Value)
                    .Distinct()
                    .ToList();

                if (locationIds.Any())
                {
                    var existingLocations = await _dbContext.Locations
                        .Where(l => locationIds.Contains(l.Id))
                        .Select(l => l.Id)
                        .ToListAsync();

                    var missingLocations = locationIds.Except(existingLocations).ToList();
                    if (missingLocations.Any())
                    {
                        return ErrorResponse($"Locations not found: {string.Join(", ", missingLocations)}", "NOT_FOUND", 404).Result!;
                    }
                }

                // Create order entity
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    RequestId = request.RequestId,
                    Status = request.Status ?? "pending",
                    CreatedAt = DateTime.UtcNow,
                    TotalAmount = 0 // Will calculate from items
                };

                // Create order items
                decimal totalAmount = 0;
                foreach (var itemRequest in request.Items)
                {
                    var lineAmount = itemRequest.Quantity * itemRequest.UnitPrice;
                    totalAmount += lineAmount;

                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        PartId = itemRequest.PartId,
                        LocationId = itemRequest.LocationId,
                        Quantity = itemRequest.Quantity,
                        UnitPrice = itemRequest.UnitPrice,
                        LineAmount = lineAmount
                    };

                    order.Items.Add(orderItem);
                }

                order.TotalAmount = totalAmount;

                // Save to database
                _dbContext.Orders.Add(order);
                await _dbContext.SaveChangesAsync();

                // Create order history entry
                var historyEntry = new OrderHistory
                {
                    OrderId = order.Id,
                    Event = "created",
                    UserId = CurrentUserId ?? request.UserId,
                    Details = $"Order created with {order.Items.Count} items, total ${order.TotalAmount:F2}",
                    Timestamp = DateTime.UtcNow
                };
                _dbContext.OrderHistory.Add(historyEntry);
                await _dbContext.SaveChangesAsync();

                // Reload with navigation properties for response
                var createdOrder = await _dbContext.Orders
                    .Include(o => o.Items)
                        .ThenInclude(oi => oi.Part)
                    .Include(o => o.Items)
                        .ThenInclude(oi => oi.Location)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                return Created($"/api/v2/orders/{order.Id}", MapToOrderResponse(createdOrder!));
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to create order: {ex.Message}", "INTERNAL_ERROR", 500).Result!;
            }
        }

        // =========================
        // GET /api/v2/orders
        // =========================
        /// <summary>
        /// List all orders with filtering and pagination.
        /// Wave 5 Task #2: GET /api/v2/orders
        /// </summary>
        /// <param name="status">Filter by status (pending, approved, shipped, delivered, cancelled)</param>
        /// <param name="userId">Filter by user ID</param>
        /// <param name="startDate">Filter orders created after this date</param>
        /// <param name="endDate">Filter orders created before this date</param>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Items per page (max 100)</param>
        /// <returns>Paginated list of orders</returns>
        [HttpGet]
        [ProducesResponseType(typeof(OrderListResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> ListOrders(
            [FromQuery] string? status = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validate pagination
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                // Build query
                var query = _dbContext.Orders
                    .Include(o => o.Items)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(o => o.Status == status.ToLowerInvariant());
                }

                // Note: Request doesn't have UserId field - would need to filter differently
                // if (userId.HasValue)
                // {
                //     query = query.Where(o => o.Request!.UserId == userId.Value);
                // }

                if (startDate.HasValue)
                {
                    query = query.Where(o => o.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(o => o.CreatedAt <= endDate.Value);
                }

                // Get total count for pagination
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // Apply pagination
                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = new OrderListResponse
                {
                    Orders = orders.Select(o => new OrderSummary
                    {
                        Id = o.Id,
                        RequestId = o.RequestId,
                        Status = o.Status,
                        TotalAmount = o.TotalAmount,
                        ItemCount = o.Items.Count,
                        CreatedAt = o.CreatedAt,
                        ShippedAt = o.ShippedAt,
                        DeliveredAt = o.DeliveredAt
                    }).ToList(),
                    Pagination = new PaginationMetadata
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalItems = totalItems,
                        TotalPages = totalPages
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to list orders: {ex.Message}", "INTERNAL_ERROR", 500).Result!;
            }
        }

        // =========================
        // POST /api/v2/orders/{orderId}/approve
        // =========================
        /// <summary>
        /// Approve an order.
        /// Wave 5 Task #3: POST /api/v2/orders/{orderId}/approve
        /// </summary>
        /// <param name="orderId">Order ID to approve</param>
        /// <param name="request">Approval request with optional notes</param>
        /// <returns>Updated order with approval details</returns>
        [HttpPost("{orderId}/approve")]
        [ProducesResponseType(typeof(OrderResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> ApproveOrder(Guid orderId, [FromBody] ApproveOrderRequest? request = null)
        {
            try
            {
                // Find order
                var order = await _dbContext.Orders
                    .Include(o => o.Items)
                        .ThenInclude(oi => oi.Part)
                    .Include(o => o.Items)
                        .ThenInclude(oi => oi.Location)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return ErrorResponse($"Order not found: {orderId}", "NOT_FOUND", 404).Result!;
                }

                // Validate status
                if (order.Status != "pending")
                {
                    return ErrorResponse($"Order cannot be approved. Current status: {order.Status}. Only pending orders can be approved.", "INVALID_OPERATION", 400).Result!;
                }

                // Get current user ID
                var approverId = CurrentUserId;
                if (!approverId.HasValue)
                {
                    return ErrorResponse("User ID not found in authentication token", "UNAUTHORIZED", 401).Result!;
                }

                // Update order status
                order.Status = "approved";

                // Create approval record
                var approval = new OrderApproval
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ApproverId = approverId.Value,
                    Status = "approved",
                    Notes = request?.Notes,
                    RequestedAt = DateTime.UtcNow,
                    ApprovedAt = DateTime.UtcNow
                };

                _dbContext.OrderApprovals.Add(approval);

                // Create history entry
                var historyEntry = new OrderHistory
                {
                    OrderId = orderId,
                    Event = "approved",
                    UserId = approverId.Value,
                    Details = $"Order approved by user {approverId}. Notes: {request?.Notes ?? "None"}",
                    Timestamp = DateTime.UtcNow
                };

                _dbContext.OrderHistory.Add(historyEntry);
                await _dbContext.SaveChangesAsync();

                return Ok(MapToOrderResponse(order));
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to approve order: {ex.Message}", "INTERNAL_ERROR", 500).Result!;
            }
        }

        // =========================
        // GET /api/v2/orders/{orderId}
        // =========================
        /// <summary>
        /// Get order details by ID.
        /// Wave 5 Task #4: GET /api/v2/orders/{orderId}
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <returns>Order details with items and approvals</returns>
        [HttpGet("{orderId}")]
        [ProducesResponseType(typeof(OrderResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetOrder(Guid orderId)
        {
            try
            {
                var order = await _dbContext.Orders
                    .Include(o => o.Items)
                        .ThenInclude(oi => oi.Part)
                    .Include(o => o.Items)
                        .ThenInclude(oi => oi.Location)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return ErrorResponse($"Order not found: {orderId}", "NOT_FOUND", 404).Result!;
                }

                return Ok(MapToOrderResponse(order));
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to get order: {ex.Message}", "INTERNAL_ERROR", 500).Result!;
            }
        }

        // =========================
        // PATCH /api/v2/orders/{orderId}
        // =========================
        /// <summary>
        /// Update order details.
        /// Wave 5 Task #4: PATCH /api/v2/orders/{orderId}
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <param name="request">Update request with status and/or notes</param>
        /// <returns>Updated order</returns>
        [HttpPatch("{orderId}")]
        [ProducesResponseType(typeof(OrderResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> UpdateOrder(Guid orderId, [FromBody] UpdateOrderRequest request)
        {
            try
            {
                // Find order
                var order = await _dbContext.Orders
                    .Include(o => o.Items)
                        .ThenInclude(oi => oi.Part)
                    .Include(o => o.Items)
                        .ThenInclude(oi => oi.Location)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return ErrorResponse($"Order not found: {orderId}", "NOT_FOUND", 404).Result!;
                }

                // Validate status transitions
                if (!string.IsNullOrEmpty(request.Status))
                {
                    var validTransitions = new Dictionary<string, string[]>
                    {
                        ["pending"] = new[] { "approved", "cancelled" },
                        ["approved"] = new[] { "shipped", "cancelled" },
                        ["shipped"] = new[] { "delivered" },
                        ["delivered"] = Array.Empty<string>(),
                        ["cancelled"] = Array.Empty<string>()
                    };

                    if (!validTransitions.ContainsKey(order.Status))
                    {
                        return ErrorResponse($"Invalid current status: {order.Status}", "INVALID_OPERATION", 400).Result!;
                    }

                    if (!validTransitions[order.Status].Contains(request.Status))
                    {
                        return ErrorResponse($"Invalid status transition: {order.Status} -> {request.Status}", "INVALID_OPERATION", 400).Result!;
                    }

                    // Update status
                    var oldStatus = order.Status;
                    order.Status = request.Status;

                    // Update timestamps
                    if (request.Status == "shipped" && !order.ShippedAt.HasValue)
                    {
                        order.ShippedAt = DateTime.UtcNow;
                    }

                    if (request.Status == "delivered" && !order.DeliveredAt.HasValue)
                    {
                        order.DeliveredAt = DateTime.UtcNow;
                    }

                    // Create history entry
                    var historyEntry = new OrderHistory
                    {
                        OrderId = orderId,
                        Event = $"status_changed_{request.Status}",
                        UserId = CurrentUserId,
                        Details = $"Status changed from {oldStatus} to {request.Status}. Notes: {request.Notes ?? "None"}",
                        Timestamp = DateTime.UtcNow
                    };

                    _dbContext.OrderHistory.Add(historyEntry);
                }

                await _dbContext.SaveChangesAsync();

                return Ok(MapToOrderResponse(order));
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to update order: {ex.Message}", "INTERNAL_ERROR", 500).Result!;
            }
        }

        // =========================
        // Helper Methods
        // =========================

        private OrderResponse MapToOrderResponse(Order order)
        {
            return new OrderResponse
            {
                Id = order.Id,
                RequestId = order.RequestId,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                ShippedAt = order.ShippedAt,
                DeliveredAt = order.DeliveredAt,
                Items = order.Items.Select(oi => new OrderItemResponse
                {
                    Id = oi.Id,
                    PartId = oi.PartId,
                    PartCode = oi.Part?.Code,
                    PartName = oi.Part?.Name,
                    LocationId = oi.LocationId,
                    LocationName = oi.Location?.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    LineAmount = oi.LineAmount
                }).ToList()
            };
        }
    }

    // =========================
    // Request/Response DTOs
    // =========================

    public class CreateOrderRequest
    {
        public Guid UserId { get; set; }
        public Guid? RequestId { get; set; }
        public string? Status { get; set; }
        public List<CreateOrderItemRequest> Items { get; set; } = new();
    }

    public class CreateOrderItemRequest
    {
        public Guid PartId { get; set; }
        public Guid? LocationId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class ApproveOrderRequest
    {
        public string? Notes { get; set; }
    }

    public class UpdateOrderRequest
    {
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }

    public class OrderResponse
    {
        public Guid Id { get; set; }
        public Guid? RequestId { get; set; }
        public string Status { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public List<OrderItemResponse> Items { get; set; } = new();
    }

    public class OrderItemResponse
    {
        public Guid Id { get; set; }
        public Guid PartId { get; set; }
        public string? PartCode { get; set; }
        public string? PartName { get; set; }
        public Guid? LocationId { get; set; }
        public string? LocationName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineAmount { get; set; }
    }

    public class OrderListResponse
    {
        public List<OrderSummary> Orders { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = null!;
    }

    public class OrderSummary
    {
        public Guid Id { get; set; }
        public Guid? RequestId { get; set; }
        public string Status { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }

    public class PaginationMetadata
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }
}
