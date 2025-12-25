using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudWatcher.Data;
using CloudWatcher.Models;
using CloudWatcher.Services;

namespace CloudWatcher.Controllers
{
    /// <summary>
    /// API v2 endpoints for checking request processing status.
    /// Provides lightweight status checks for polling clients.
    /// </summary>
    [ApiController]
    [Route("api/v2/requests")]
    [Produces("application/json")]
    public class RequestStatusControllerV2 : ControllerBase
    {
        private readonly CloudWatcherContext _dbContext;
        private readonly WebSocketMessageRouter _wsRouter;
        private readonly ILogger<RequestStatusControllerV2> _logger;

        public RequestStatusControllerV2(
            CloudWatcherContext dbContext,
            WebSocketMessageRouter wsRouter,
            ILogger<RequestStatusControllerV2> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _wsRouter = wsRouter ?? throw new ArgumentNullException(nameof(wsRouter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET /api/v2/requests/{requestId}/status
        /// Get lightweight status information for a request.
        /// Designed for efficient polling without full request content.
        /// </summary>
        /// <param name="requestId">The request ID (UUID format)</param>
        /// <returns>200 OK with status information, 404 if not found</returns>
        /// <response code="200">Status retrieved successfully</response>
        /// <response code="400">Invalid request ID format</response>
        /// <response code="404">Request not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{requestId}/status")]
        public async Task<ActionResult<RequestStatusResponse>> GetRequestStatusAsync(string requestId)
        {
            try
            {
                // Validate request ID format (must be valid UUID)
                if (!Guid.TryParse(requestId, out var requestGuid))
                {
                    _logger.LogWarning("Invalid request ID format: {RequestId}", requestId);
                    return BadRequest(new ErrorResponse
                    {
                        Message = $"Invalid request ID format: '{requestId}'. Must be a valid UUID."
                    });
                }

                _logger.LogInformation("Retrieving status for request: {RequestId}", requestGuid);

                // Query request (read-only)
                var request = await _dbContext.Requests.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == requestGuid);

                if (request == null)
                {
                    _logger.LogWarning("Request not found: {RequestId}", requestGuid);
                    return NotFound(new ErrorResponse
                    {
                        Message = $"Request with ID '{requestGuid}' not found"
                    });
                }

                // Build status response
                var response = new RequestStatusResponse
                {
                    RequestId = request.Id,
                    Status = request.Status,
                    StatusUpdatedAt = request.UpdatedAt ?? request.CreatedAt,
                    CreatedAt = request.CreatedAt,
                    Progress = BuildProgressInfo(request.Status)
                };

                _logger.LogInformation(
                    "Successfully retrieved status for request {RequestId}. Status: {Status}",
                    requestGuid, request.Status);

                return Ok(response);
            }
            catch (ArgumentException argEx)
            {
                _logger.LogError(argEx, "Argument error retrieving request status");
                return BadRequest(new ErrorResponse
                {
                    Message = "Invalid request parameter format"
                });
            }
            catch (OperationCanceledException opEx)
            {
                _logger.LogError(opEx, "Operation timeout retrieving request status");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new ErrorResponse { Message = "Request processing timeout" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving request status for {RequestId}", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = "An unexpected error occurred while retrieving request status" });
            }
        }

        /// <summary>
        /// Updates request status and notifies connected WebSocket clients.
        /// Internal API for request processing pipeline.
        /// </summary>
        /// <param name="requestId">The request ID</param>
        /// <param name="newStatus">The new status</param>
        /// <param name="deviceId">Device ID for WebSocket notification (optional)</param>
        /// <returns>200 OK if update successful</returns>
        [HttpPut("{requestId}/status")]
        [ApiExplorerSettings(IgnoreApi = true)] // Internal endpoint
        public async Task<IActionResult> UpdateRequestStatusAsync(
            string requestId,
            [FromQuery] string newStatus,
            [FromQuery] string? deviceId = null)
        {
            try
            {
                // Validate request ID format
                if (!Guid.TryParse(requestId, out var requestGuid))
                {
                    _logger.LogWarning("Invalid request ID format: {RequestId}", requestId);
                    return BadRequest(new ErrorResponse { Message = "Invalid request ID format" });
                }

                if (string.IsNullOrWhiteSpace(newStatus))
                {
                    return BadRequest(new ErrorResponse { Message = "Status cannot be empty" });
                }

                // Get request
                var request = await _dbContext.Requests.FirstOrDefaultAsync(r => r.Id == requestGuid);
                if (request == null)
                {
                    return NotFound(new ErrorResponse { Message = "Request not found" });
                }

                // Update status
                var oldStatus = request.Status;
                request.Status = newStatus;
                request.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Request {RequestId} status updated: {OldStatus} -> {NewStatus}",
                    requestGuid, oldStatus, newStatus);

                // Notify WebSocket clients if device ID is provided
                if (!string.IsNullOrWhiteSpace(deviceId))
                {
                    try
                    {
                        await _wsRouter.NotifyRequestStatusChangeAsync(
                            deviceId,
                            requestId,
                            newStatus,
                            new { previousStatus = oldStatus, updatedAt = request.UpdatedAt });

                        _logger.LogDebug(
                            "WebSocket notification sent for request {RequestId} to device {DeviceId}",
                            requestGuid, deviceId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to send WebSocket notification for request {RequestId}",
                            requestGuid);
                        // Don't fail the request if WebSocket notification fails
                    }
                }

                return Ok(new { message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating request status: {RequestId}", requestId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Build progress information based on current request status.
        /// Provides stage-specific details for client UI updates.
        /// </summary>
        private ProgressInfo BuildProgressInfo(string status)
        {
            return status.ToLower() switch
            {
                "pending" => new ProgressInfo
                {
                    Stage = "queued",
                    Percentage = 0,
                    CurrentAgent = null,
                    EstimatedTimeRemaining = 300 // 5 minutes
                },
                "processing" => new ProgressInfo
                {
                    Stage = "ai_processing",
                    Percentage = 50,
                    CurrentAgent = "RequestRouter",
                    EstimatedTimeRemaining = 120 // 2 minutes
                },
                "completed" => new ProgressInfo
                {
                    Stage = "completed",
                    Percentage = 100,
                    CurrentAgent = null,
                    EstimatedTimeRemaining = 0
                },
                "failed" => new ProgressInfo
                {
                    Stage = "failed",
                    Percentage = 0,
                    CurrentAgent = null,
                    EstimatedTimeRemaining = 0
                },
                "expired" => new ProgressInfo
                {
                    Stage = "expired",
                    Percentage = 0,
                    CurrentAgent = null,
                    EstimatedTimeRemaining = 0
                },
                _ => new ProgressInfo
                {
                    Stage = "unknown",
                    Percentage = 0,
                    CurrentAgent = null,
                    EstimatedTimeRemaining = 60
                }
            };
        }
    }

    /// <summary>
    /// Response containing request status information.
    /// </summary>
    public class RequestStatusResponse
    {
        /// <summary>
        /// Unique request ID
        /// </summary>
        public Guid RequestId { get; set; }

        /// <summary>
        /// Current request status: pending, processing, completed, failed, expired
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// When the status was last updated
        /// </summary>
        public DateTime StatusUpdatedAt { get; set; }

        /// <summary>
        /// Progress details including stage and percentage
        /// </summary>
        public ProgressInfo Progress { get; set; } = null!;

        /// <summary>
        /// When the request was originally created
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Progress information for a request.
    /// </summary>
    public class ProgressInfo
    {
        /// <summary>
        /// Current processing stage: queued, ai_processing, completed, failed, expired
        /// </summary>
        public string Stage { get; set; } = null!;

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int Percentage { get; set; }

        /// <summary>
        /// Currently processing agent (null if not applicable)
        /// </summary>
        public string? CurrentAgent { get; set; }

        /// <summary>
        /// Estimated seconds remaining until completion (0 if complete/failed)
        /// </summary>
        public int EstimatedTimeRemaining { get; set; }
    }
}
