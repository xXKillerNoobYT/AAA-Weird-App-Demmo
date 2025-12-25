using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using CloudWatcher.Data;
using CloudWatcher.Models;

namespace CloudWatcher.Controllers
{
    /// <summary>
    /// API v2 endpoints for submitting and managing device requests.
    /// Handles request submission with validation, queuing, and response tracking.
    /// </summary>
    [ApiController]
    [Route("api/v2/requests")]
    [Produces("application/json")]
    public class RequestSubmissionControllerV2 : ControllerBase
    {
        private readonly CloudWatcherContext _dbContext;
        private readonly ILogger<RequestSubmissionControllerV2> _logger;

        public RequestSubmissionControllerV2(CloudWatcherContext dbContext, ILogger<RequestSubmissionControllerV2> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// POST /api/v2/requests/submit
        /// Submit a new device request for processing.
        /// </summary>
        /// <param name="request">Request payload with deviceId, type, and metadata</param>
        /// <returns>202 Accepted with request ID for tracking</returns>
        /// <response code="202">Request accepted and queued for processing</response>
        /// <response code="400">Invalid request format or missing required fields</response>
        /// <response code="422">Request validation failed (invalid type, schema error)</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("submit")]
        public async Task<ActionResult<SubmitRequestResponse>> SubmitRequestAsync(
            [FromBody] SubmitRequestPayload request)
        {
            try
            {
                // Validate input is not null
                if (request == null)
                {
                    _logger.LogWarning("Submit request received with null payload");
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Request payload cannot be null"
                    });
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.DeviceId))
                {
                    _logger.LogWarning("Submit request missing deviceId");
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Field 'deviceId' is required"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Type))
                {
                    _logger.LogWarning("Submit request missing type");
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Field 'type' is required"
                    });
                }

                // Validate request type
                var validTypes = new[] { "get_parts", "order_parts", "status_check", "parts_inquiry", "supplier_lookup" };
                if (!validTypes.Contains(request.Type.ToLower()))
                {
                    _logger.LogWarning("Submit request with invalid type: {Type}", request.Type);
                    return UnprocessableEntity(new ErrorResponse
                    {
                        Message = $"Invalid request type: '{request.Type}'. Must be one of: {string.Join(", ", validTypes)}"
                    });
                }

                // Validate payload schema if provided
                if (request.Payload != null && !IsValidPayloadSchema(request.Payload))
                {
                    _logger.LogWarning("Submit request with invalid payload schema for type: {Type}", request.Type);
                    return UnprocessableEntity(new ErrorResponse
                    {
                        Message = "Payload schema validation failed. Check payload structure for your request type."
                    });
                }

                _logger.LogInformation("Processing submit request from device {DeviceId} with type {Type}", 
                    request.DeviceId, request.Type);

                // Create new request entity
                var newRequest = new Request
                {
                    Id = Guid.NewGuid(),
                    DeviceId = request.DeviceId.Trim(),
                    Type = request.Type.ToLower(),
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null
                };

                // Add to database
                _dbContext.Requests.Add(newRequest);

                // Add metadata if provided
                if (request.Metadata != null && request.Metadata.Count > 0)
                {
                    foreach (var kvp in request.Metadata)
                    {
                        var metadata = new RequestMetadata
                        {
                            Id = Guid.NewGuid(),
                            RequestId = newRequest.Id,
                            Key = kvp.Key,
                            Value = kvp.Value
                        };
                        _dbContext.RequestMetadata.Add(metadata);
                    }
                }

                // Save to database
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Request submitted successfully. RequestId: {RequestId}, DeviceId: {DeviceId}, Type: {Type}, MetadataCount: {MetadataCount}",
                    newRequest.Id, newRequest.DeviceId, newRequest.Type, 
                    request.Metadata?.Count ?? 0);

                // Return 202 Accepted with request ID for polling
                var response = new SubmitRequestResponse
                {
                    RequestId = newRequest.Id,
                    Status = "pending",
                    Message = "Request submitted successfully and queued for processing",
                    CreatedAt = newRequest.CreatedAt,
                    StatusCheckUri = $"/api/v2/requests/{newRequest.Id}/status"
                };

                return Accepted(response.StatusCheckUri, response);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error submitting request");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = "Failed to save request to database" });
            }
            catch (InvalidOperationException invEx)
            {
                _logger.LogError(invEx, "Invalid operation error submitting request");
                return BadRequest(new ErrorResponse
                {
                    Message = "Invalid request operation"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error submitting request");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = "An unexpected error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Validate payload schema based on request type.
        /// </summary>
        private bool IsValidPayloadSchema(Dictionary<string, JsonElement> payload)
        {
            if (payload == null || payload.Count == 0)
                return true;

            try
            {
                // Basic validation - ensure payload is properly structured
                // In production, use JSON Schema validation library
                foreach (var kvp in payload)
                {
                    if (string.IsNullOrEmpty(kvp.Key))
                        return false;
                    // Validate that value is not null
                    if (kvp.Value.ValueKind == JsonValueKind.Null)
                        return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Request submission payload DTO
    /// </summary>
    public class SubmitRequestPayload
    {
        /// <summary>
        /// Device ID that is submitting the request
        /// </summary>
        public string DeviceId { get; set; } = null!;

        /// <summary>
        /// Request type: get_parts, order_parts, status_check, parts_inquiry, supplier_lookup
        /// </summary>
        public string Type { get; set; } = null!;

        /// <summary>
        /// Optional metadata as key-value pairs
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Optional payload with request-specific data
        /// </summary>
        public Dictionary<string, JsonElement>? Payload { get; set; }
    }

    /// <summary>
    /// Response to a successful request submission
    /// </summary>
    public class SubmitRequestResponse
    {
        /// <summary>
        /// Unique request ID for tracking and polling
        /// </summary>
        public Guid RequestId { get; set; }

        /// <summary>
        /// Current status of the request
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// Human-readable success message
        /// </summary>
        public string Message { get; set; } = null!;

        /// <summary>
        /// Timestamp when request was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// URI to check request status
        /// </summary>
        public string StatusCheckUri { get; set; } = null!;
    }

}
