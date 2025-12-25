using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using CloudWatcher.Data;
using CloudWatcher.Models;

namespace CloudWatcher.Controllers
{
    /// <summary>
    /// API v2 endpoints for retrieving request responses.
    /// Provides access to AI agent processing results and decisions.
    /// </summary>
    [ApiController]
    [Route("api/v2/responses")]
    [Produces("application/json")]
    public class ResponsesControllerV2 : ControllerBase
    {
        private readonly CloudWatcherContext _dbContext;
        private readonly ILogger<ResponsesControllerV2> _logger;

        public ResponsesControllerV2(CloudWatcherContext dbContext, ILogger<ResponsesControllerV2> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET /api/v2/responses/{requestId}
        /// Get the response for a specific request.
        /// Returns AI agent processing results, decisions, and generated content.
        /// </summary>
        /// <param name="requestId">The request ID (UUID format)</param>
        /// <returns>200 OK with response content, 202 Accepted if processing, 404 if not found</returns>
        /// <response code="200">Response ready and content available</response>
        /// <response code="202">Request is still processing, response not yet available</response>
        /// <response code="400">Invalid request ID format</response>
        /// <response code="404">Request or response not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{requestId}")]
        public async Task<ActionResult<GetResponseData>> GetResponseAsync(string requestId)
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

                _logger.LogInformation("Retrieving response for request: {RequestId}", requestGuid);

                // Verify request exists
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

                // Get response for this request
                var response = await _dbContext.Responses.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RequestId == requestGuid);

                if (response == null)
                {
                    _logger.LogWarning("Response not found for request: {RequestId}", requestGuid);
                    return NotFound(new ErrorResponse
                    {
                        Message = $"No response found for request '{requestGuid}'"
                    });
                }

                // Check if response is still processing
                if (response.Status.ToLower() == "pending")
                {
                    _logger.LogInformation("Response still processing for request: {RequestId}", requestGuid);
                    // Return 202 Accepted - response is being generated
                    return AcceptedAtAction(null, new GetResponseData
                    {
                        ResponseId = response.Id,
                        RequestId = response.RequestId,
                        Status = "processing",
                        Content = null,
                        DeliveredAt = null,
                        CreatedAt = response.CreatedAt
                    });
                }

                // Parse response content
                var responseContent = ParseResponseContent(response.Content);

                var result = new GetResponseData
                {
                    ResponseId = response.Id,
                    RequestId = response.RequestId,
                    Status = response.Status,
                    Content = responseContent,
                    DeliveredAt = response.UpdatedAt,
                    CreatedAt = response.CreatedAt
                };

                _logger.LogInformation(
                    "Successfully retrieved response for request {RequestId}. Status: {Status}",
                    requestGuid, response.Status);

                return Ok(result);
            }
            catch (ArgumentException argEx)
            {
                _logger.LogError(argEx, "Argument error retrieving response");
                return BadRequest(new ErrorResponse
                {
                    Message = "Invalid request parameter format"
                });
            }
            catch (OperationCanceledException opEx)
            {
                _logger.LogError(opEx, "Operation timeout retrieving response");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new ErrorResponse { Message = "Request processing timeout" });
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON parsing error in response content");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = "Error parsing response content" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving response for {RequestId}", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = "An unexpected error occurred while retrieving the response" });
            }
        }

        /// <summary>
        /// Parse response content from JSON string.
        /// Handles content generation and agent decision extraction.
        /// </summary>
        private dynamic? ParseResponseContent(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return null;

            try
            {
                // Parse JSON content
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Return as dynamic object via JsonElement
                // In production, consider creating strongly-typed response DTO
                return root;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse response content as JSON");
                return new
                {
                    parts = new object[0],
                    agentDecisions = new object[0],
                    error = "Response content is not valid JSON"
                };
            }
        }
    }

    /// <summary>
    /// Response containing AI agent processing results.
    /// </summary>
    public class GetResponseData
    {
        /// <summary>
        /// Unique response ID
        /// </summary>
        public Guid ResponseId { get; set; }

        /// <summary>
        /// The original request ID this response is for
        /// </summary>
        public Guid RequestId { get; set; }

        /// <summary>
        /// Response status: pending, sent, acknowledged, failed
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// Response content generated by AI agents.
        /// Contains parts, suppliers, decisions, and recommendations.
        /// </summary>
        public dynamic? Content { get; set; }

        /// <summary>
        /// When the response was delivered/completed
        /// </summary>
        public DateTime? DeliveredAt { get; set; }

        /// <summary>
        /// When the response record was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
