using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using CloudWatcher.RequestHandling;
using CloudWatcher.CloudStorage;

namespace CloudWatcher.Controllers
{
    /// <summary>
    /// API endpoints for managing device responses.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ResponseController : ControllerBase
    {
        private readonly RequestHandler _handler;
        private readonly ILogger<ResponseController> _logger;

        public ResponseController(RequestHandler handler, ILogger<ResponseController> logger)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Upload a device response to cloud storage.
        /// </summary>
        [HttpPost("{deviceId}/{requestId}")]
        [Produces("application/json")]
        public async Task<ActionResult> UploadResponseAsync(
            string deviceId,
            string requestId,
            [FromBody] DeviceResponse response)
        {
            try
            {
                _logger.LogInformation("Uploading response {RequestId} for device {DeviceId}", requestId, deviceId);

                var result = await _handler.ProcessOutgoingResponseAsync(deviceId, requestId, response);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to upload response: {Message}", result.Message);
                    return BadRequest(new ErrorResponse { Message = result.Message });
                }

                _logger.LogInformation("Successfully uploaded response {RequestId}", requestId);
                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception uploading response {RequestId}", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieve a device response from cloud storage.
        /// </summary>
        [HttpGet("{deviceId}/{requestId}")]
        [Produces("application/json")]
        public async Task<ActionResult<DeviceResponse>> GetResponseAsync(string deviceId, string requestId)
        {
            try
            {
                _logger.LogInformation("Retrieving response {RequestId} for device {DeviceId}", requestId, deviceId);

                var result = await _handler.GetOutgoingResponseAsync(deviceId, requestId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to retrieve response: {Message}", result.Message);
                    return NotFound(new ErrorResponse { Message = result.Message });
                }

                if (result.Data is not DeviceResponse response)
                {
                    _logger.LogWarning("Retrieved data is not a DeviceResponse");
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ErrorResponse { Message = "Invalid response data format" });
                }

                _logger.LogInformation("Successfully retrieved response {RequestId}", requestId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception retrieving response {RequestId}", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a device response from cloud storage.
        /// </summary>
        [HttpDelete("{deviceId}/{requestId}")]
        public async Task<IActionResult> DeleteResponseAsync(string deviceId, string requestId)
        {
            try
            {
                _logger.LogInformation("Deleting response {RequestId} for device {DeviceId}", requestId, deviceId);

                var result = await _handler.DeleteResponseAsync(deviceId, requestId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to delete response: {Message}", result.Message);
                    return BadRequest(new ErrorResponse { Message = result.Message });
                }

                _logger.LogInformation("Successfully deleted response {RequestId}", requestId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception deleting response {RequestId}", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// List all responses for a device.
        /// </summary>
        [HttpGet("{deviceId}")]
        [Produces("application/json")]
        public async Task<ActionResult<List<CloudFile>>> ListResponsesAsync(string deviceId)
        {
            try
            {
                _logger.LogInformation("Listing responses for device {DeviceId}", deviceId);

                var result = await _handler.ListResponsesAsync(deviceId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to list responses: {Message}", result.Message);
                    return BadRequest(new ErrorResponse { Message = result.Message });
                }

                if (result.Data is not List<CloudFile> files)
                {
                    _logger.LogWarning("Listed data is not a List<CloudFile>");
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ErrorResponse { Message = "Invalid response data format" });
                }

                _logger.LogInformation("Successfully listed {Count} responses for device {DeviceId}",
                    files.Count, deviceId);
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception listing responses for device {DeviceId}", deviceId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = ex.Message });
            }
        }
    }
}
