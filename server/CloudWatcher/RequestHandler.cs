using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CloudWatcher.CloudStorage;

namespace CloudWatcher.RequestHandling
{
    /// <summary>
    /// Handles device request/response processing with cloud storage integration.
    /// Manages upload/download of requests and responses using ICloudStorageProvider.
    /// </summary>
    public class RequestHandler
    {
        private readonly ICloudStorageProvider _storageProvider;
        private readonly RequestHandlerOptions _options;

        public RequestHandler(ICloudStorageProvider storageProvider, RequestHandlerOptions? options = null)
        {
            _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
            _options = options ?? new RequestHandlerOptions();
        }

        /// <summary>
        /// Processes an incoming request from a device and uploads it to cloud storage.
        /// </summary>
        public async Task<CloudOperationResult> ProcessIncomingRequestAsync(
            string deviceId,
            string requestId,
            DeviceRequest request)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return CloudOperationResult.CreateFailure("Device ID cannot be empty");

            if (request == null)
                return CloudOperationResult.CreateFailure("Request object cannot be null");

            try
            {
                // Serialize the request to JSON
                var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var requestBytes = System.Text.Encoding.UTF8.GetBytes(requestJson);
                var requestFileName = $"{requestId}.json";
                var folderPath = $"/Cloud/Requests/{deviceId}";

                // Ensure folder exists
                await _storageProvider.CreateFolderAsync("/Cloud/Requests", deviceId);

                // Upload with retry logic
                var result = await RetryAsync(
                    async () => await _storageProvider.UploadFileAsync(folderPath, requestFileName, requestBytes),
                    _options.MaxRetries);

                if (!result.Success)
                {
                    return CloudOperationResult.CreateFailure(
                        $"Failed to upload request after {_options.MaxRetries} retries: {result.Message}",
                        result.Exception);
                }

                return CloudOperationResult.CreateSuccess(new
                {
                    RequestId = requestId,
                    DeviceId = deviceId,
                    UploadedPath = $"{folderPath}/{requestFileName}",
                    Timestamp = DateTime.UtcNow.ToString("o")
                });
            }
            catch (Exception ex)
            {
                return CloudOperationResult.CreateFailure(
                    $"Error processing incoming request: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Processes an outgoing response to a device and uploads it to cloud storage.
        /// </summary>
        public async Task<CloudOperationResult> ProcessOutgoingResponseAsync(
            string deviceId,
            string requestId,
            DeviceResponse response)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return CloudOperationResult.CreateFailure("Device ID cannot be empty");

            if (response == null)
                return CloudOperationResult.CreateFailure("Response object cannot be null");

            try
            {
                // Serialize the response to JSON
                var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var responseBytes = System.Text.Encoding.UTF8.GetBytes(responseJson);
                var responseFileName = $"{requestId}.json";
                var folderPath = $"/Cloud/Responses/{deviceId}";

                // Ensure folder exists
                await _storageProvider.CreateFolderAsync("/Cloud/Responses", deviceId);

                // Upload with retry logic
                var result = await RetryAsync(
                    async () => await _storageProvider.UploadFileAsync(folderPath, responseFileName, responseBytes),
                    _options.MaxRetries);

                if (!result.Success)
                {
                    return CloudOperationResult.CreateFailure(
                        $"Failed to upload response after {_options.MaxRetries} retries: {result.Message}",
                        result.Exception);
                }

                return CloudOperationResult.CreateSuccess(new
                {
                    RequestId = requestId,
                    DeviceId = deviceId,
                    UploadedPath = $"{folderPath}/{responseFileName}",
                    Timestamp = DateTime.UtcNow.ToString("o")
                });
            }
            catch (Exception ex)
            {
                return CloudOperationResult.CreateFailure(
                    $"Error processing outgoing response: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Retrieves a device request from cloud storage.
        /// </summary>
        public async Task<CloudOperationResult> GetIncomingRequestAsync(
            string deviceId,
            string requestId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return CloudOperationResult.CreateFailure("Device ID cannot be empty");

            if (string.IsNullOrWhiteSpace(requestId))
                return CloudOperationResult.CreateFailure("Request ID cannot be empty");

            try
            {
                var requestFileName = $"{requestId}.json";
                var folderPath = $"/Cloud/Requests/{deviceId}";

                // Download with retry logic
                var result = await RetryAsync(
                    async () => await _storageProvider.DownloadFileAsync(folderPath, requestFileName),
                    _options.MaxRetries);

                if (!result.Success)
                {
                    return CloudOperationResult.CreateFailure(
                        $"Failed to download request: {result.Message}",
                        result.Exception);
                }

                // Parse the downloaded JSON
                if (result.Data is byte[] fileContent)
                {
                    var jsonString = System.Text.Encoding.UTF8.GetString(fileContent);
                    var request = JsonSerializer.Deserialize<DeviceRequest>(jsonString);

                    return CloudOperationResult.CreateSuccess(request);
                }

                return CloudOperationResult.CreateFailure("Downloaded file content is invalid");
            }
            catch (Exception ex)
            {
                return CloudOperationResult.CreateFailure(
                    $"Error retrieving incoming request: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Retrieves a device response from cloud storage.
        /// </summary>
        public async Task<CloudOperationResult> GetOutgoingResponseAsync(
            string deviceId,
            string requestId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return CloudOperationResult.CreateFailure("Device ID cannot be empty");

            if (string.IsNullOrWhiteSpace(requestId))
                return CloudOperationResult.CreateFailure("Request ID cannot be empty");

            try
            {
                var responseFileName = $"{requestId}.json";
                var folderPath = $"/Cloud/Responses/{deviceId}";

                // Download with retry logic
                var result = await RetryAsync(
                    async () => await _storageProvider.DownloadFileAsync(folderPath, responseFileName),
                    _options.MaxRetries);

                if (!result.Success)
                {
                    return CloudOperationResult.CreateFailure(
                        $"Failed to download response: {result.Message}",
                        result.Exception);
                }

                // Parse the downloaded JSON
                if (result.Data is byte[] fileContent)
                {
                    var jsonString = System.Text.Encoding.UTF8.GetString(fileContent);
                    var response = JsonSerializer.Deserialize<DeviceResponse>(jsonString);

                    return CloudOperationResult.CreateSuccess(response);
                }

                return CloudOperationResult.CreateFailure("Downloaded file content is invalid");
            }
            catch (Exception ex)
            {
                return CloudOperationResult.CreateFailure(
                    $"Error retrieving outgoing response: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Deletes a request from cloud storage.
        /// </summary>
        public async Task<CloudOperationResult> DeleteRequestAsync(
            string deviceId,
            string requestId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return CloudOperationResult.CreateFailure("Device ID cannot be empty");

            if (string.IsNullOrWhiteSpace(requestId))
                return CloudOperationResult.CreateFailure("Request ID cannot be empty");

            try
            {
                var requestFileName = $"{requestId}.json";
                var folderPath = $"/Cloud/Requests/{deviceId}";

                var result = await RetryAsync(
                    async () => await _storageProvider.DeleteFileAsync(folderPath, requestFileName),
                    _options.MaxRetries);

                return result;
            }
            catch (Exception ex)
            {
                return CloudOperationResult.CreateFailure(
                    $"Error deleting request: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Lists all requests for a specific device.
        /// </summary>
        public async Task<CloudOperationResult> ListRequestsAsync(string deviceId, string filePattern = "*.json")
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return CloudOperationResult.CreateFailure("Device ID cannot be empty");

            try
            {
                var folderPath = $"/Cloud/Requests/{deviceId}";
                var result = await RetryAsync(
                    async () => await _storageProvider.ListFilesAsync(folderPath, filePattern),
                    _options.MaxRetries);

                return result;
            }
            catch (Exception ex)
            {
                return CloudOperationResult.CreateFailure(
                    $"Error listing requests: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Lists all responses for a specific device.
        /// </summary>
        public async Task<CloudOperationResult> ListResponsesAsync(string deviceId, string filePattern = "*.json")
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return CloudOperationResult.CreateFailure("Device ID cannot be empty");

            try
            {
                var folderPath = $"/Cloud/Responses/{deviceId}";
                var result = await RetryAsync(
                    async () => await _storageProvider.ListFilesAsync(folderPath, filePattern),
                    _options.MaxRetries);

                return result;
            }
            catch (Exception ex)
            {
                return CloudOperationResult.CreateFailure(
                    $"Error listing responses: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Deletes a response from cloud storage.
        /// </summary>
        public async Task<CloudOperationResult> DeleteResponseAsync(
            string deviceId,
            string requestId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return CloudOperationResult.CreateFailure("Device ID cannot be empty");

            if (string.IsNullOrWhiteSpace(requestId))
                return CloudOperationResult.CreateFailure("Request ID cannot be empty");

            try
            {
                var responseFileName = $"{requestId}.json";
                var folderPath = $"/Cloud/Responses/{deviceId}";

                var result = await RetryAsync(
                    async () => await _storageProvider.DeleteFileAsync(folderPath, responseFileName),
                    _options.MaxRetries);

                return result;
            }
            catch (Exception ex)
            {
                return CloudOperationResult.CreateFailure(
                    $"Error deleting response: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Executes an async operation with exponential backoff retry logic.
        /// </summary>
        private async Task<CloudOperationResult> RetryAsync(
            Func<Task<CloudOperationResult>> operation,
            int maxRetries)
        {
            int retryCount = 0;
            int delayMs = _options.InitialRetryDelayMs;
            Exception? lastException = null;
            CloudOperationResult? lastResult = null;

            while (retryCount <= maxRetries)
            {
                try
                {
                    var result = await operation();
                    
                    // If operation succeeded, return immediately
                    if (result.Success)
                    {
                        return result;
                    }
                    
                    // Operation failed, prepare for retry
                    lastResult = result;
                    lastException = result.Exception;
                    retryCount++;

                    if (retryCount > maxRetries)
                    {
                        break;
                    }

                    // Calculate exponential backoff with jitter
                    var jitter = new Random().Next(
                        (int)(delayMs * (1 - _options.RetryJitterFactor)),
                        (int)(delayMs * (1 + _options.RetryJitterFactor)));

                    await Task.Delay(jitter);

                    // Exponential backoff
                    delayMs = (int)(delayMs * _options.RetryBackoffMultiplier);
                    if (delayMs > _options.MaxRetryDelayMs)
                        delayMs = _options.MaxRetryDelayMs;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;

                    if (retryCount > maxRetries)
                    {
                        break;
                    }

                    // Calculate exponential backoff with jitter
                    var jitter = new Random().Next(
                        (int)(delayMs * (1 - _options.RetryJitterFactor)),
                        (int)(delayMs * (1 + _options.RetryJitterFactor)));

                    await Task.Delay(jitter);

                    // Exponential backoff
                    delayMs = (int)(delayMs * _options.RetryBackoffMultiplier);
                    if (delayMs > _options.MaxRetryDelayMs)
                        delayMs = _options.MaxRetryDelayMs;
                }
            }

            return CloudOperationResult.CreateFailure(
                $"Operation failed after {maxRetries} retries",
                lastException);
        }
    }

    /// <summary>
    /// Configuration options for RequestHandler retry behavior.
    /// </summary>
    public class RequestHandlerOptions
    {
        /// <summary>
        /// Maximum number of retry attempts (default: 3).
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Initial retry delay in milliseconds (default: 100ms).
        /// </summary>
        public int InitialRetryDelayMs { get; set; } = 100;

        /// <summary>
        /// Maximum retry delay in milliseconds (default: 5000ms).
        /// </summary>
        public int MaxRetryDelayMs { get; set; } = 5000;

        /// <summary>
        /// Exponential backoff multiplier (default: 2.0).
        /// </summary>
        public double RetryBackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Jitter factor for randomization (default: 0.1 = ±10%).
        /// </summary>
        public double RetryJitterFactor { get; set; } = 0.1;
    }

    /// <summary>
    /// Represents an incoming device request.
    /// </summary>
    public class DeviceRequest
    {
        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        [JsonPropertyName("device_id")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("request_type")]
        public string? RequestType { get; set; }

        [JsonPropertyName("payload")]
        public JsonElement? Payload { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; } = "1.0";
    }

    /// <summary>
    /// Represents an outgoing device response.
    /// </summary>
    public class DeviceResponse
    {
        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        [JsonPropertyName("device_id")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("data")]
        public JsonElement? Data { get; set; }

        [JsonPropertyName("error")]
        public ErrorDetail? Error { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("server_build_signature")]
        public string? ServerBuildSignature { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; } = "1.0";
    }

    /// <summary>
    /// Represents error details in a device response.
    /// </summary>
    public class ErrorDetail
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("details")]
        public JsonElement? Details { get; set; }
    }
}
