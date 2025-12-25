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
    /// API v2 endpoints for managing device requests with database persistence.
    /// Implements comprehensive request retrieval with pagination, filtering, and error handling.
    /// </summary>
    [ApiController]
    [Route("api/v2/requests")]
    [Produces("application/json")]
    public class RequestsControllerV2 : ControllerBase
    {
        private readonly CloudWatcherContext _dbContext;
        private readonly ILogger<RequestsControllerV2> _logger;

        public RequestsControllerV2(CloudWatcherContext dbContext, ILogger<RequestsControllerV2> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET /api/v2/requests/{requestId}
        /// Retrieve a single request by ID with all nested properties.
        /// </summary>
        /// <param name="requestId">The request ID (UUID)</param>
        /// <returns>Request record with nested metadata, responses, and cloud file references</returns>
        /// <response code="200">Request found and returned</response>
        /// <response code="400">Invalid request ID format</response>
        /// <response code="404">Request not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{requestId}")]
        public async Task<ActionResult<GetRequestResponse>> GetRequestAsync(string requestId)
        {
            try
            {
                // Validate request ID format
                if (!Guid.TryParse(requestId, out var requestGuid))
                {
                    _logger.LogWarning("Invalid request ID format: {RequestId}", requestId);
                    return BadRequest(new ErrorResponse
                    {
                        Message = $"Invalid request ID format: {requestId}. Expected UUID format."
                    });
                }

                _logger.LogInformation("Retrieving request {RequestId}", requestGuid);

                // Query request with all nested properties
                var request = await _dbContext.Requests
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == requestGuid);

                if (request == null)
                {
                    _logger.LogWarning("Request not found: {RequestId}", requestGuid);
                    return NotFound(new ErrorResponse
                    {
                        Message = $"Request '{requestGuid}' not found."
                    });
                }

                // Fetch related metadata
                var metadata = await _dbContext.RequestMetadata
                    .AsNoTracking()
                    .Where(m => m.RequestId == requestGuid)
                    .ToListAsync();

                // Fetch related cloud file references
                var cloudReferences = await _dbContext.CloudFileReferences
                    .AsNoTracking()
                    .Where(cf => cf.RequestId == requestGuid)
                    .ToListAsync();

                // Fetch related responses
                var responses = await _dbContext.Responses
                    .AsNoTracking()
                    .Where(r => r.RequestId == requestGuid)
                    .ToListAsync();

                _logger.LogInformation(
                    "Successfully retrieved request {RequestId}. Metadata: {MetadataCount}, CloudRefs: {CloudRefCount}, Responses: {ResponseCount}",
                    requestGuid, metadata.Count, cloudReferences.Count, responses.Count);

                var response = new GetRequestResponse
                {
                    Id = request.Id,
                    DeviceId = request.DeviceId,
                    Type = request.Type,
                    Status = request.Status,
                    CreatedAt = request.CreatedAt,
                    UpdatedAt = request.UpdatedAt,
                    Payload = new RequestPayload
                    {
                        Metadata = metadata.ToDictionary(m => m.Key, m => m.Value)
                    },
                    Metadata = metadata.Select(m => new MetadataItem
                    {
                        Key = m.Key,
                        Value = m.Value
                    }).ToList(),
                    CloudFileReferences = cloudReferences.Select(cf => new CloudFileReferenceDto
                    {
                        Id = cf.Id,
                        CloudPath = cf.CloudPath,
                        Provider = cf.Provider,
                        CreatedAt = cf.CreatedAt
                    }).ToList(),
                    RelatedResponses = responses.Select(r => new RelatedResponseDto
                    {
                        Id = r.Id,
                        Status = r.Status,
                        Content = r.Content,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    }).ToList()
                };

                return Ok(response);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error retrieving request {RequestId}", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = "Database error retrieving request." });
            }
            catch (OperationCanceledException opEx)
            {
                _logger.LogError(opEx, "Operation timeout retrieving request {RequestId}", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = "Request operation timed out." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving request {RequestId}", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = "An unexpected error occurred while retrieving the request." });
            }
        }

        /// <summary>
        /// Standard error response format.
        /// </summary>
        public class ErrorResponse
        {
            public string? Message { get; set; }
        }
    }

    /// <summary>
    /// Response DTO for GET /api/v2/requests/{requestId}
    /// </summary>
    public class GetRequestResponse
    {
        public Guid Id { get; set; }
        public string DeviceId { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Request payload with nested metadata as key-value pairs
        /// </summary>
        public RequestPayload Payload { get; set; } = new();

        /// <summary>
        /// Array of metadata key-value pairs
        /// </summary>
        public List<MetadataItem> Metadata { get; set; } = new();

        /// <summary>
        /// Cloud file references associated with this request
        /// </summary>
        public List<CloudFileReferenceDto> CloudFileReferences { get; set; } = new();

        /// <summary>
        /// Related responses to this request
        /// </summary>
        public List<RelatedResponseDto> RelatedResponses { get; set; } = new();
    }

    /// <summary>
    /// Request payload structure
    /// </summary>
    public class RequestPayload
    {
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Metadata key-value pair
    /// </summary>
    public class MetadataItem
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    /// <summary>
    /// Cloud file reference DTO
    /// </summary>
    public class CloudFileReferenceDto
    {
        public Guid Id { get; set; }
        public string CloudPath { get; set; } = null!;
        public string Provider { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Related response DTO
    /// </summary>
    public class RelatedResponseDto
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = null!;
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
