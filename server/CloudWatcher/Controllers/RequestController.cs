using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using CloudWatcher.RequestHandling;
using CloudWatcher.CloudStorage;

namespace CloudWatcher.Controllers
{
    /// <summary>
    /// API endpoints for managing device requests.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RequestController : ControllerBase
    {
        private readonly RequestHandler _handler;
        private readonly ILogger<RequestController> _logger;

        public RequestController(RequestHandler handler, ILogger<RequestController> logger)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Upload a device request to cloud storage.
        /// </summary>
        [HttpPost("{deviceId}/{requestId}")]
        [Produces("application/json")]
        public async Task<ActionResult> UploadRequestAsync(
            string deviceId,
            string requestId,
            [FromBody] DeviceRequest request)
        {
            try
            {
                _logger.LogInformation("Uploading request {RequestId} for device {DeviceId}", requestId, deviceId);

                var result = await _handler.ProcessIncomingRequestAsync(deviceId, requestId, request);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to upload request: {Message}", result.Message);
                    return BadRequest(new ErrorResponse { Message = result.Message });
                }

                _logger.LogInformation("Successfully uploaded request {RequestId}", requestId);
                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception uploading request {RequestId}", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieve a device request from cloud storage.
        /// </summary>
        [HttpGet("{deviceId}/{requestId}")]
        [Produces("application/json")]
        public async Task<ActionResult<DeviceRequest>> GetRequestAsync(string deviceId, string requestId)
        {
            try
            {
                _logger.LogInformation("Retrieving request {RequestId} for device {DeviceId}", requestId, deviceId);

                var result = await _handler.GetIncomingRequestAsync(deviceId, requestId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to retrieve request: {Message}", result.Message);
                    return NotFound(new ErrorResponse { Message = result.Message });
                }

                if (result.Data is not DeviceRequest request)
                {
                    _logger.LogWarning("Retrieved data is not a DeviceRequest");
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ErrorResponse { Message = "Invalid request data format" });
                }

                _logger.LogInformation("Successfully retrieved request {RequestId}", requestId);
                return Ok(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception retrieving request {RequestId}", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a device request from cloud storage.
        /// </summary>
        [HttpDelete("{deviceId}/{requestId}")]
        public async Task<IActionResult> DeleteRequestAsync(string deviceId, string requestId)
        {
            try
            {
                _logger.LogInformation("Deleting request {RequestId} for device {DeviceId}", requestId, deviceId);

                var result = await _handler.DeleteRequestAsync(deviceId, requestId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to delete request: {Message}", result.Message);
                    return BadRequest(new ErrorResponse { Message = result.Message });
                }

                _logger.LogInformation("Successfully deleted request {RequestId}", requestId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception deleting request {RequestId}", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// List all requests for a device.
        /// </summary>
        [HttpGet("{deviceId}")]
        [Produces("application/json")]
        public async Task<ActionResult<List<CloudFile>>> ListRequestsAsync(string deviceId)
        {
            try
            {
                _logger.LogInformation("Listing requests for device {DeviceId}", deviceId);

                var result = await _handler.ListRequestsAsync(deviceId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to list requests: {Message}", result.Message);
                    return BadRequest(new ErrorResponse { Message = result.Message });
                }

                if (result.Data is not List<CloudFile> files)
                {
                    _logger.LogWarning("Listed data is not a List<CloudFile>");
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ErrorResponse { Message = "Invalid response data format" });
                }

                _logger.LogInformation("Successfully listed {Count} requests for device {DeviceId}", 
                    files.Count, deviceId);
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception listing requests for device {DeviceId}", deviceId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = ex.Message });
            }
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
