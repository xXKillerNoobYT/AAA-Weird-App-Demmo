using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudWatcher.Data;
using CloudWatcher.Models;

namespace CloudWatcher.Controllers
{
    /// <summary>
    /// API v2 endpoints for querying and retrieving device requests.
    /// Handles request listing with filtering, sorting, and pagination.
    /// </summary>
    [ApiController]
    [Route("api/v2/requests")]
    [Produces("application/json")]
    public class RequestsListingControllerV2 : ControllerBase
    {
        private readonly CloudWatcherContext _dbContext;
        private readonly ILogger<RequestsListingControllerV2> _logger;

        public RequestsListingControllerV2(CloudWatcherContext dbContext, ILogger<RequestsListingControllerV2> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET /api/v2/requests
        /// List all requests with optional filtering, sorting, and pagination.
        /// </summary>
        /// <param name="status">Filter by status: pending, processing, completed, failed</param>
        /// <param name="deviceId">Filter by device ID</param>
        /// <param name="requestType">Filter by request type</param>
        /// <param name="createdAfter">Filter requests created after this ISO 8601 timestamp</param>
        /// <param name="limit">Number of results to return (1-100, default 20)</param>
        /// <param name="offset">Pagination offset (default 0)</param>
        /// <returns>200 OK with paginated request list</returns>
        /// <response code="200">Requests retrieved successfully</response>
        /// <response code="400">Invalid query parameters</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        public async Task<ActionResult<ListRequestsResponse>> ListRequestsAsync(
            [FromQuery] string? status,
            [FromQuery] string? deviceId,
            [FromQuery] string? requestType,
            [FromQuery] string? createdAfter,
            [FromQuery(Name = "limit")] int limit = 20,
            [FromQuery(Name = "offset")] int offset = 0)
        {
            try
            {
                // Validate pagination parameters
                if (limit < 1 || limit > 100)
                {
                    _logger.LogWarning("Invalid limit parameter: {Limit}. Must be between 1 and 100", limit);
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Parameter 'limit' must be between 1 and 100"
                    });
                }

                if (offset < 0)
                {
                    _logger.LogWarning("Invalid offset parameter: {Offset}. Must be >= 0", offset);
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Parameter 'offset' must be >= 0"
                    });
                }

                // Validate status filter if provided
                var validStatuses = new[] { "pending", "processing", "completed", "failed" };
                if (!string.IsNullOrEmpty(status) && !validStatuses.Contains(status.ToLower()))
                {
                    _logger.LogWarning("Invalid status parameter: {Status}", status);
                    return BadRequest(new ErrorResponse
                    {
                        Message = $"Invalid status: '{status}'. Must be one of: {string.Join(", ", validStatuses)}"
                    });
                }

                // Parse createdAfter timestamp if provided
                DateTime? createdAfterDate = null;
                if (!string.IsNullOrEmpty(createdAfter))
                {
                    if (!DateTime.TryParse(createdAfter, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsedDate))
                    {
                        _logger.LogWarning("Invalid createdAfter parameter: {CreatedAfter}", createdAfter);
                        return BadRequest(new ErrorResponse
                        {
                            Message = "Parameter 'createdAfter' must be a valid ISO 8601 timestamp"
                        });
                    }
                    createdAfterDate = parsedDate;
                }

                _logger.LogInformation(
                    "Listing requests with filters - Status: {Status}, DeviceId: {DeviceId}, Type: {Type}, CreatedAfter: {CreatedAfter}, Limit: {Limit}, Offset: {Offset}",
                    status ?? "all", deviceId ?? "all", requestType ?? "all", createdAfter ?? "none", limit, offset);

                // Build query
                IQueryable<Request> query = _dbContext.Requests.AsNoTracking();

                // Apply filters
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(r => r.Status.ToLower() == status.ToLower());
                }

                if (!string.IsNullOrEmpty(deviceId))
                {
                    query = query.Where(r => r.DeviceId == deviceId);
                }

                if (!string.IsNullOrEmpty(requestType))
                {
                    query = query.Where(r => r.Type.ToLower() == requestType.ToLower());
                }

                if (createdAfterDate.HasValue)
                {
                    query = query.Where(r => r.CreatedAt >= createdAfterDate.Value);
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply sorting (most recent first) and pagination
                var requests = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip(offset)
                    .Take(limit)
                    .Include(r => r.RequestMetadata)
                    .Include(r => r.CloudFileReferences)
                    .Include(r => r.Responses)
                    .ToListAsync();

                // Map to DTOs
                var requestDtos = requests.Select(r => new RequestListItemDto
                {
                    RequestId = r.Id,
                    DeviceId = r.DeviceId,
                    Type = r.Type,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    MetadataCount = r.RequestMetadata?.Count ?? 0,
                    ResponseCount = r.Responses?.Count ?? 0,
                    CloudFileReferenceCount = r.CloudFileReferences?.Count ?? 0
                }).ToList();

                var response = new ListRequestsResponse
                {
                    Requests = requestDtos,
                    Pagination = new PaginationInfo
                    {
                        Total = totalCount,
                        Limit = limit,
                        Offset = offset,
                        HasMore = (offset + limit) < totalCount
                    }
                };

                _logger.LogInformation(
                    "Successfully retrieved {Count} requests. Total: {Total}, HasMore: {HasMore}",
                    requestDtos.Count, totalCount, response.Pagination.HasMore);

                return Ok(response);
            }
            catch (ArgumentException argEx)
            {
                _logger.LogError(argEx, "Argument error listing requests");
                return BadRequest(new ErrorResponse
                {
                    Message = "Invalid query parameter format"
                });
            }
            catch (OperationCanceledException opEx)
            {
                _logger.LogError(opEx, "Operation timeout listing requests");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new ErrorResponse { Message = "Request processing timeout" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error listing requests");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = "An unexpected error occurred while retrieving requests" });
            }
        }
    }

    /// <summary>
    /// Paginated list of requests
    /// </summary>
    public class ListRequestsResponse
    {
        /// <summary>
        /// Array of request items
        /// </summary>
        public List<RequestListItemDto> Requests { get; set; } = new();

        /// <summary>
        /// Pagination metadata
        /// </summary>
        public PaginationInfo Pagination { get; set; } = new();
    }

    /// <summary>
    /// Compact request item for list responses
    /// </summary>
    public class RequestListItemDto
    {
        /// <summary>
        /// Unique request ID
        /// </summary>
        public Guid RequestId { get; set; }

        /// <summary>
        /// Device that submitted the request
        /// </summary>
        public string DeviceId { get; set; } = null!;

        /// <summary>
        /// Request type (get_parts, order_parts, status_check, etc.)
        /// </summary>
        public string Type { get; set; } = null!;

        /// <summary>
        /// Current request status
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// When the request was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the request was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Count of metadata entries
        /// </summary>
        public int MetadataCount { get; set; }

        /// <summary>
        /// Count of responses
        /// </summary>
        public int ResponseCount { get; set; }

        /// <summary>
        /// Count of cloud file references
        /// </summary>
        public int CloudFileReferenceCount { get; set; }
    }

}
