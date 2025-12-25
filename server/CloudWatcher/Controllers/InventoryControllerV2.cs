using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudWatcher.Data;
using CloudWatcher.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloudWatcher.Controllers
{
    /// <summary>
    /// Inventory Management API - Wave 4
    /// Provides endpoints for viewing and managing inventory across locations
    /// </summary>
    [ApiController]
    [Route("api/v2/inventory")]
    [Authorize]
    public class InventoryControllerV2 : ControllerBase
    {
        private readonly CloudWatcherContext _dbContext;
        private readonly ILogger<InventoryControllerV2> _logger;

        /// <summary>
        /// Initializes a new instance of the InventoryControllerV2
        /// </summary>
        public InventoryControllerV2(CloudWatcherContext dbContext, ILogger<InventoryControllerV2> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/v2/inventory - List parts with advanced filtering and pagination
        /// </summary>
        /// <param name="category">Filter by part category (optional)</param>
        /// <param name="partName">Filter by part name (substring search, optional)</param>
        /// <param name="locationId">Filter by specific location ID (optional)</param>
        /// <param name="inStock">Filter by in-stock status: true/false (optional)</param>
        /// <param name="lowStock">Filter by low-stock status: true/false (optional)</param>
        /// <param name="page">Page number for pagination (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
        /// <returns>Paginated list of parts with inventory information</returns>
        /// <response code="200">Returns paginated list of parts with inventory data</response>
        /// <response code="400">Bad request - invalid parameters</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(InventoryListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InventoryListResponse>> GetInventory(
            [FromQuery] string? category = null,
            [FromQuery] string? partName = null,
            [FromQuery] string? locationId = null,
            [FromQuery] bool? inStock = null,
            [FromQuery] bool? lowStock = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validate pagination parameters
                if (page < 1 || pageSize < 1 || pageSize > 100)
                {
                    _logger.LogWarning("Invalid pagination parameters: page={page}, pageSize={pageSize}", page, pageSize);
                    return BadRequest(new { error = "page must be >= 1, pageSize must be between 1 and 100" });
                }

                _logger.LogInformation("Getting inventory with filters: category={category}, partName={partName}, locationId={locationId}, inStock={inStock}, lowStock={lowStock}, page={page}, pageSize={pageSize}",
                    category, partName, locationId, inStock, lowStock, page, pageSize);

                // Parse locationId if provided
                Guid? locationGuid = null;
                if (!string.IsNullOrWhiteSpace(locationId))
                {
                    if (!Guid.TryParse(locationId, out var parsedLocationId))
                    {
                        _logger.LogWarning("Invalid location ID format: {locationId}", locationId);
                        return BadRequest(new { error = "locationId must be a valid UUID" });
                    }
                    locationGuid = parsedLocationId;
                }

                // Build base query - get all parts
                var query = _dbContext.Parts.AsQueryable();

                // Apply category filter
                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(p => p.Category != null && p.Category.Contains(category));
                    _logger.LogDebug("Applied category filter: {category}", category);
                }

                // Apply part name filter (substring search)
                if (!string.IsNullOrWhiteSpace(partName))
                {
                    query = query.Where(p => p.Name.Contains(partName));
                    _logger.LogDebug("Applied partName filter: {partName}", partName);
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var paginatedQuery = query
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                // Fetch parts with their inventory data
                var parts = await paginatedQuery
                    .Include(p => p.Code)
                    .ToListAsync();

                // Project into response DTOs with inventory data
                var items = new List<InventoryItemDto>();

                foreach (var part in parts)
                {
                    // Get inventory for this part
                    var inventoryQuery = _dbContext.Inventory
                        .Where(i => i.PartId == part.Id)
                        .AsQueryable();

                    // Apply location filter if specified
                    if (locationGuid.HasValue)
                    {
                        inventoryQuery = inventoryQuery.Where(i => i.LocationId == locationGuid);
                    }

                    var inventoryRecords = await inventoryQuery
                        .Include(i => i.Location)
                        .ToListAsync();

                    // Apply inStock filter if specified
                    if (inStock.HasValue)
                    {
                        if (inStock.Value)
                        {
                            inventoryRecords = inventoryRecords
                                .Where(i => i.QuantityOnHand > 0)
                                .ToList();
                        }
                        else
                        {
                            inventoryRecords = inventoryRecords
                                .Where(i => i.QuantityOnHand == 0)
                                .ToList();
                        }
                    }

                    // Apply lowStock filter if specified
                    if (lowStock.HasValue)
                    {
                        if (lowStock.Value)
                        {
                            inventoryRecords = inventoryRecords
                                .Where(i => i.QuantityOnHand < i.ReorderLevel)
                                .ToList();
                        }
                        else
                        {
                            inventoryRecords = inventoryRecords
                                .Where(i => i.QuantityOnHand >= i.ReorderLevel)
                                .ToList();
                        }
                    }

                    // Get variant count
                    var variantCount = await _dbContext.PartVariants
                        .Where(pv => pv.PartId == part.Id)
                        .CountAsync();

                    // Calculate totals
                    var totalQuantity = inventoryRecords.Sum(i => i.QuantityOnHand);
                    var totalLocations = inventoryRecords.Count;

                    // Build location breakdown
                    var locationBreakdown = inventoryRecords
                        .Select(i => new LocationInventoryDto
                        {
                            LocationId = i.LocationId.ToString(),
                            LocationName = i.Location?.Name ?? "Unknown",
                            QuantityOnHand = i.QuantityOnHand,
                            ReorderLevel = i.ReorderLevel,
                            IsLowStock = i.QuantityOnHand < i.ReorderLevel
                        })
                        .ToList();

                    items.Add(new InventoryItemDto
                    {
                        PartId = part.Id.ToString(),
                        PartCode = part.Code,
                        PartName = part.Name,
                        Description = part.Description,
                        Category = part.Category,
                        StandardPrice = part.StandardPrice,
                        TotalQuantityOnHand = totalQuantity,
                        TotalLocations = totalLocations,
                        VariantCount = variantCount,
                        Locations = locationBreakdown,
                        LastUpdated = inventoryRecords.Max(i => i.LastInventoryCheck)
                    });
                }

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var response = new InventoryListResponse
                {
                    Items = items,
                    Pagination = new PaginationMetadata
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = totalCount,
                        TotalPages = totalPages,
                        HasNextPage = page < totalPages,
                        HasPreviousPage = page > 1
                    }
                };

                _logger.LogInformation("Inventory query successful: {itemCount} items returned", items.Count);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving inventory list");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/v2/inventory/{partId} - Get part details with all variants and inventory
        /// </summary>
        /// <param name="partId">Part UUID identifier</param>
        /// <param name="includeArchived">Include archived variants (default: false)</param>
        /// <returns>Part with variants and location inventory</returns>
        /// <response code="200">Returns part with variants and inventory</response>
        /// <response code="400">Invalid part ID format</response>
        /// <response code="404">Part not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{partId}")]
        [ProducesResponseType(typeof(PartDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PartDetailResponse>> GetPartById(
            [FromRoute] string partId,
            [FromQuery] bool includeArchived = false)
        {
            try
            {
                // Parse part ID
                if (!Guid.TryParse(partId, out var partGuid))
                {
                    _logger.LogWarning("Invalid part ID format: {partId}", partId);
                    return BadRequest(new { error = "partId must be a valid UUID" });
                }

                _logger.LogInformation("Getting part details for partId={partId}, includeArchived={includeArchived}", partGuid, includeArchived);

                // Get part with variants
                var part = await _dbContext.Parts
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == partGuid);

                if (part == null)
                {
                    _logger.LogWarning("Part not found: {partId}", partGuid);
                    return NotFound(new { error = "Part not found" });
                }

                // Get inventory across all locations
                var inventoryRecords = await _dbContext.Inventory
                    .Where(i => i.PartId == partGuid)
                    .Include(i => i.Location)
                    .ToListAsync();

                // Build variant DTOs
                var variants = part.Variants
                    .Where(v => includeArchived || !v.VariantCode.StartsWith("ARCHIVED_"))
                    .Select(v => new PartVariantDto
                    {
                        VariantId = v.Id.ToString(),
                        VariantCode = v.VariantCode,
                        Attributes = v.Attributes ?? "{}",
                        VariantPrice = v.VariantPrice ?? 0m,
                        CreatedAt = v.CreatedAt
                    })
                    .ToList();

                // Build location inventory breakdown
                var locations = inventoryRecords
                    .Select(i => new LocationInventoryDto
                    {
                        LocationId = i.LocationId.ToString(),
                        LocationName = i.Location?.Name ?? "Unknown",
                        QuantityOnHand = i.QuantityOnHand,
                        ReorderLevel = i.ReorderLevel,
                        IsLowStock = i.QuantityOnHand < i.ReorderLevel
                    })
                    .ToList();

                var response = new PartDetailResponse
                {
                    PartId = part.Id.ToString(),
                    PartCode = part.Code,
                    PartName = part.Name,
                    Description = part.Description ?? string.Empty,
                    Category = part.Category ?? string.Empty,
                    StandardPrice = part.StandardPrice,
                    CreatedAt = part.CreatedAt,
                    UpdatedAt = part.UpdatedAt ?? DateTime.UtcNow,
                    TotalQuantityOnHand = inventoryRecords.Sum(i => i.QuantityOnHand),
                    Variants = variants,
                    Locations = locations,
                    LastInventoryCheck = inventoryRecords.Count > 0 
                        ? inventoryRecords.Max(i => i.LastInventoryCheck) 
                        : DateTime.UtcNow
                };

                _logger.LogInformation("Retrieved part {partId} with {variantCount} variants and {locationCount} locations",
                    partGuid, variants.Count, locations.Count);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving part {partId}", partId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// PATCH /api/v2/inventory/{partId} - Update inventory with audit trail
        /// </summary>
        /// <param name="partId">Part UUID identifier</param>
        /// <param name="updateRequest">Inventory update request payload</param>
        /// <returns>Updated inventory with audit log entry</returns>
        /// <response code="200">Inventory updated successfully</response>
        /// <response code="400">Invalid request or part ID format</response>
        /// <response code="404">Part or location not found</response>
        /// <response code="403">Insufficient permissions (requires DeptManager or higher)</response>
        /// <response code="500">Internal server error</response>
        [HttpPatch("{partId}")]
        [Authorize(Policy = "DeptManager")]
        [ProducesResponseType(typeof(InventoryUpdateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InventoryUpdateResponse>> UpdateInventory(
            [FromRoute] string partId,
            [FromBody] InventoryUpdateRequest updateRequest)
        {
            try
            {
                // Parse part ID
                if (!Guid.TryParse(partId, out var partGuid))
                {
                    _logger.LogWarning("Invalid part ID format: {partId}", partId);
                    return BadRequest(new { error = "partId must be a valid UUID" });
                }

                // Validate request
                if (updateRequest == null)
                {
                    return BadRequest(new { error = "Request body is required" });
                }

                if (!Guid.TryParse(updateRequest.LocationId, out var locationGuid))
                {
                    _logger.LogWarning("Invalid location ID format: {locationId}", updateRequest.LocationId);
                    return BadRequest(new { error = "locationId must be a valid UUID" });
                }

                _logger.LogInformation("Updating inventory for partId={partId}, locationId={locationId}", 
                    partGuid, locationGuid);

                // Verify part exists
                var part = await _dbContext.Parts.FindAsync(partGuid);
                if (part == null)
                {
                    _logger.LogWarning("Part not found: {partId}", partGuid);
                    return NotFound(new { error = "Part not found" });
                }

                // Verify location exists
                var location = await _dbContext.Locations.FindAsync(locationGuid);
                if (location == null)
                {
                    _logger.LogWarning("Location not found: {locationId}", locationGuid);
                    return NotFound(new { error = "Location not found" });
                }

                // Find or create inventory record
                var inventory = await _dbContext.Inventory
                    .FirstOrDefaultAsync(i => i.PartId == partGuid && i.LocationId == locationGuid);

                bool isNewRecord = inventory == null;
                if (isNewRecord)
                {
                    inventory = new Inventory
                    {
                        PartId = partGuid,
                        LocationId = locationGuid,
                        QuantityOnHand = 0,
                        ReorderLevel = 10,
                        ReorderQuantity = 20,
                        LastInventoryCheck = DateTime.UtcNow
                    };
                    _dbContext.Inventory.Add(inventory);
                }

                // Track old values for audit
                var oldQuantity = inventory.QuantityOnHand;
                var oldReorderLevel = inventory.ReorderLevel;
                var oldReorderQuantity = inventory.ReorderQuantity;

                // Apply updates (partial update support)
                if (updateRequest.QuantityOnHand.HasValue)
                {
                    inventory.QuantityOnHand = updateRequest.QuantityOnHand.Value;
                }
                if (updateRequest.ReorderLevel.HasValue)
                {
                    inventory.ReorderLevel = updateRequest.ReorderLevel.Value;
                }
                if (updateRequest.ReorderQuantity.HasValue)
                {
                    inventory.ReorderQuantity = updateRequest.ReorderQuantity.Value;
                }
                inventory.LastInventoryCheck = DateTime.UtcNow;

                // Create audit log entry
                var auditLog = new InventoryAuditLog
                {
                    InventoryId = inventory.Id,
                    PartId = partGuid,
                    LocationId = locationGuid,
                    ChangeType = isNewRecord ? "CREATE" : "UPDATE",
                    OldQuantity = oldQuantity,
                    NewQuantity = inventory.QuantityOnHand,
                    OldReorderLevel = oldReorderLevel,
                    NewReorderLevel = inventory.ReorderLevel,
                    ChangedBy = User.Identity?.Name ?? "System",
                    ChangedAt = DateTime.UtcNow,
                    Notes = updateRequest.Notes ?? $"Inventory {(isNewRecord ? "created" : "updated")} via API"
                };

                _dbContext.InventoryAuditLogs.Add(auditLog);

                // Save changes
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Inventory updated: partId={partId}, locationId={locationId}, oldQty={oldQty}, newQty={newQty}", 
                    partGuid, locationGuid, oldQuantity, inventory.QuantityOnHand);

                var response = new InventoryUpdateResponse
                {
                    Success = true,
                    Message = isNewRecord ? "Inventory record created" : "Inventory updated successfully",
                    InventoryId = inventory.Id.ToString(),
                    PartId = partGuid.ToString(),
                    PartCode = part.Code,
                    PartName = part.Name,
                    LocationId = locationGuid.ToString(),
                    LocationName = location.Name,
                    QuantityOnHand = inventory.QuantityOnHand,
                    ReorderLevel = inventory.ReorderLevel,
                    ReorderQuantity = inventory.ReorderQuantity,
                    LastInventoryCheck = inventory.LastInventoryCheck,
                    AuditLogId = auditLog.Id.ToString(),
                    ChangedBy = auditLog.ChangedBy,
                    ChangedAt = auditLog.ChangedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory for part {partId}", partId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/v2/inventory/{partId}/availability - Calculate available quantity
        /// </summary>
        /// <param name="partId">Part UUID identifier</param>
        /// <param name="includeReserved">Include reserved quantities breakdown (default: false)</param>
        /// <returns>Part availability across all locations with reserved quantities</returns>
        /// <response code="200">Returns availability information</response>
        /// <response code="400">Invalid part ID format</response>
        /// <response code="404">Part not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{partId}/availability")]
        [ProducesResponseType(typeof(PartAvailabilityResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PartAvailabilityResponse>> GetPartAvailability(
            [FromRoute] string partId,
            [FromQuery] bool includeReserved = false)
        {
            try
            {
                // Parse part ID
                if (!Guid.TryParse(partId, out var partGuid))
                {
                    _logger.LogWarning("Invalid part ID format: {partId}", partId);
                    return BadRequest(new { error = "partId must be a valid UUID" });
                }

                _logger.LogInformation("Getting availability for partId={partId}, includeReserved={includeReserved}", 
                    partGuid, includeReserved);

                // Get part
                var part = await _dbContext.Parts.FindAsync(partGuid);
                if (part == null)
                {
                    _logger.LogWarning("Part not found: {partId}", partGuid);
                    return NotFound(new { error = "Part not found" });
                }

                // Get all inventory across locations
                var inventoryRecords = await _dbContext.Inventory
                    .Where(i => i.PartId == partGuid)
                    .Include(i => i.Location)
                    .ToListAsync();

                // TODO: Reserved quantities calculation requires Order.Status and OrderItem.LocationId
                // For now, set reserved to 0 until Order model is enhanced
                var reservedByLocation = new Dictionary<Guid, int>();

                // Calculate availability by location
                var locationAvailability = inventoryRecords
                    .Select(i =>
                    {
                        var reserved = reservedByLocation.GetValueOrDefault(i.LocationId, 0);
                        return new LocationAvailabilityDto
                        {
                            LocationId = i.LocationId.ToString(),
                            LocationName = i.Location?.Name ?? "Unknown",
                            QuantityOnHand = i.QuantityOnHand,
                            ReservedQuantity = reserved,
                            AvailableQuantity = i.QuantityOnHand - reserved,
                            ReorderLevel = i.ReorderLevel,
                            IsLowStock = i.QuantityOnHand < i.ReorderLevel,
                            NeedsReorder = (i.QuantityOnHand - reserved) < i.ReorderLevel
                        };
                    })
                    .ToList();

                var totalOnHand = locationAvailability.Sum(l => l.QuantityOnHand);
                var totalReserved = locationAvailability.Sum(l => l.ReservedQuantity);
                var totalAvailable = totalOnHand - totalReserved;

                var response = new PartAvailabilityResponse
                {
                    PartId = partGuid.ToString(),
                    PartCode = part.Code,
                    PartName = part.Name,
                    TotalQuantityOnHand = totalOnHand,
                    TotalReserved = totalReserved,
                    TotalAvailable = totalAvailable,
                    LocationCount = locationAvailability.Count,
                    Locations = locationAvailability,
                    CheckedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Availability calculated: partId={partId}, totalOnHand={onHand}, totalAvailable={available}",
                    partGuid, totalOnHand, totalAvailable);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving availability for part {partId}", partId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Response DTOs for inventory endpoints
        /// </summary>

        /// <summary>
        /// Detailed part response with variants and location inventory
        /// </summary>
        public class PartDetailResponse
        {
            public string PartId { get; set; } = string.Empty;
            public string PartCode { get; set; } = string.Empty;
            public string PartName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public decimal StandardPrice { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public int TotalQuantityOnHand { get; set; }
            public DateTime LastInventoryCheck { get; set; }
            public List<PartVariantDto> Variants { get; set; } = new();
            public List<LocationInventoryDto> Locations { get; set; } = new();
        }

        /// <summary>
        /// Part variant details
        /// </summary>
        public class PartVariantDto
        {
            public string VariantId { get; set; } = string.Empty;
            public string VariantCode { get; set; } = string.Empty;
            public string Attributes { get; set; } = string.Empty;
            public decimal VariantPrice { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        /// <summary>
        /// Detailed inventory item in paginated response
        /// </summary>
        public class InventoryItemDto
        {
            /// <summary>Unique part identifier</summary>
            public string PartId { get; set; } = null!;

            /// <summary>Part code (SKU)</summary>
            public string PartCode { get; set; } = null!;

            /// <summary>Part name</summary>
            public string PartName { get; set; } = null!;

            /// <summary>Part description</summary>
            public string? Description { get; set; }

            /// <summary>Part category</summary>
            public string? Category { get; set; }

            /// <summary>Standard unit price</summary>
            public decimal StandardPrice { get; set; }

            /// <summary>Total quantity on hand across all locations</summary>
            public int TotalQuantityOnHand { get; set; }

            /// <summary>Number of locations holding this part</summary>
            public int TotalLocations { get; set; }

            /// <summary>Number of part variants available</summary>
            public int VariantCount { get; set; }

            /// <summary>Breakdown by location</summary>
            public List<LocationInventoryDto> Locations { get; set; } = new();

            /// <summary>Last inventory check timestamp</summary>
            public DateTime LastUpdated { get; set; }
        }

        public class LocationInventoryDto
        {
            /// <summary>Location UUID</summary>
            public string LocationId { get; set; } = null!;

            /// <summary>Location name (warehouse, field, etc)</summary>
            public string LocationName { get; set; } = null!;

            /// <summary>Quantity on hand at this location</summary>
            public int QuantityOnHand { get; set; }

            /// <summary>Reorder level threshold</summary>
            public int ReorderLevel { get; set; }

            /// <summary>Whether inventory is below reorder level</summary>
            public bool IsLowStock { get; set; }
        }

        public class InventoryListResponse
        {
            /// <summary>List of inventory items</summary>
            public List<InventoryItemDto> Items { get; set; } = new();

            /// <summary>Pagination metadata</summary>
            public PaginationMetadata Pagination { get; set; } = null!;
        }

        public class PaginationMetadata
        {
            /// <summary>Current page number</summary>
            public int CurrentPage { get; set; }

            /// <summary>Items per page</summary>
            public int PageSize { get; set; }

            /// <summary>Total number of items</summary>
            public int TotalItems { get; set; }

            /// <summary>Total number of pages</summary>
            public int TotalPages { get; set; }

            /// <summary>Whether more pages exist after current</summary>
            public bool HasNextPage { get; set; }

            /// <summary>Whether pages exist before current</summary>
            public bool HasPreviousPage { get; set; }
        }

        /// <summary>
        /// Request payload for inventory updates
        /// </summary>
        public class InventoryUpdateRequest
        {
            public string LocationId { get; set; } = null!;
            public int? QuantityOnHand { get; set; }
            public int? ReorderLevel { get; set; }
            public int? ReorderQuantity { get; set; }
            public string? Notes { get; set; }
        }

        /// <summary>
        /// Response for inventory update operations
        /// </summary>
        public class InventoryUpdateResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string InventoryId { get; set; } = string.Empty;
            public string PartId { get; set; } = string.Empty;
            public string PartCode { get; set; } = string.Empty;
            public string PartName { get; set; } = string.Empty;
            public string LocationId { get; set; } = string.Empty;
            public string LocationName { get; set; } = string.Empty;
            public int QuantityOnHand { get; set; }
            public int ReorderLevel { get; set; }
            public int ReorderQuantity { get; set; }
            public DateTime LastInventoryCheck { get; set; }
            public string AuditLogId { get; set; } = string.Empty;
            public string ChangedBy { get; set; } = string.Empty;
            public DateTime ChangedAt { get; set; }
        }

        /// <summary>
        /// Part availability response with location breakdown
        /// </summary>
        public class PartAvailabilityResponse
        {
            public string PartId { get; set; } = string.Empty;
            public string PartCode { get; set; } = string.Empty;
            public string PartName { get; set; } = string.Empty;
            public int TotalQuantityOnHand { get; set; }
            public int TotalReserved { get; set; }
            public int TotalAvailable { get; set; }
            public int LocationCount { get; set; }
            public DateTime CheckedAt { get; set; }
            public List<LocationAvailabilityDto> Locations { get; set; } = new();
        }

        /// <summary>
        /// Location-specific availability details
        /// </summary>
        public class LocationAvailabilityDto
        {
            public string LocationId { get; set; } = string.Empty;
            public string LocationName { get; set; } = string.Empty;
            public int QuantityOnHand { get; set; }
            public int ReservedQuantity { get; set; }
            public int AvailableQuantity { get; set; }
            public int ReorderLevel { get; set; }
            public bool IsLowStock { get; set; }
            public bool NeedsReorder { get; set; }
        }
    }
}
