using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using CloudWatcher.CloudStorage;
using CloudWatcher.RequestHandling;

namespace CloudWatcher.Tests
{
    /// <summary>
    /// Unit tests for RequestHandler class.
    /// </summary>
    public class RequestHandlerTests
    {
        private readonly MockCloudStorageProvider _mockProvider;
        private readonly RequestHandler _handler;

        public RequestHandlerTests()
        {
            _mockProvider = new MockCloudStorageProvider();
            _handler = new RequestHandler(_mockProvider);
        }

        [Fact]
        public void Constructor_WithNullProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new RequestHandler(null!));
        }

        [Fact]
        public void Constructor_WithValidProvider_CreatesInstance()
        {
            var handler = new RequestHandler(_mockProvider);
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task ProcessIncomingRequestAsync_WithEmptyDeviceId_ReturnsFail()
        {
            var request = new DeviceRequest { RequestId = "req-001", RequestType = "ping" };
            var result = await _handler.ProcessIncomingRequestAsync("", "req-001", request);
            
            Assert.False(result.Success);
            Assert.Contains("Device ID", result.Message);
        }

        [Fact]
        public async Task ProcessIncomingRequestAsync_WithNullRequest_ReturnsFail()
        {
            var result = await _handler.ProcessIncomingRequestAsync("truck-001", "req-001", null!);
            
            Assert.False(result.Success);
            Assert.Contains("Request object", result.Message);
        }

        [Fact]
        public async Task ProcessIncomingRequestAsync_WithValidRequest_ReturnsSuccess()
        {
            _mockProvider.SetNextResult(CloudOperationResult.CreateSuccess(new { message = "uploaded" }));
            var request = new DeviceRequest 
            { 
                RequestId = "req-001", 
                DeviceId = "truck-001",
                RequestType = "ping",
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            
            var result = await _handler.ProcessIncomingRequestAsync("truck-001", "req-001", request);
            
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task ProcessOutgoingResponseAsync_WithEmptyDeviceId_ReturnsFail()
        {
            var response = new DeviceResponse { RequestId = "req-001", Status = "success" };
            var result = await _handler.ProcessOutgoingResponseAsync("", "req-001", response);
            
            Assert.False(result.Success);
            Assert.Contains("Device ID", result.Message);
        }

        [Fact]
        public async Task ProcessOutgoingResponseAsync_WithNullResponse_ReturnsFail()
        {
            var result = await _handler.ProcessOutgoingResponseAsync("truck-001", "req-001", null!);
            
            Assert.False(result.Success);
            Assert.Contains("Response object", result.Message);
        }

        [Fact]
        public async Task ProcessOutgoingResponseAsync_WithValidResponse_ReturnsSuccess()
        {
            _mockProvider.SetNextResult(CloudOperationResult.CreateSuccess(new { message = "uploaded" }));
            var response = new DeviceResponse 
            { 
                RequestId = "req-001",
                DeviceId = "truck-001",
                Status = "success",
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            
            var result = await _handler.ProcessOutgoingResponseAsync("truck-001", "req-001", response);
            
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task GetIncomingRequestAsync_WithEmptyDeviceId_ReturnsFail()
        {
            var result = await _handler.GetIncomingRequestAsync("", "req-001");
            
            Assert.False(result.Success);
            Assert.Contains("Device ID", result.Message);
        }

        [Fact]
        public async Task GetIncomingRequestAsync_WithEmptyRequestId_ReturnsFail()
        {
            var result = await _handler.GetIncomingRequestAsync("truck-001", "");
            
            Assert.False(result.Success);
            Assert.Contains("Request ID", result.Message);
        }

        [Fact]
        public async Task GetIncomingRequestAsync_WithValidIds_ReturnsRequest()
        {
            var requestJson = JsonSerializer.Serialize(new DeviceRequest 
            { 
                RequestId = "req-001",
                DeviceId = "truck-001",
                RequestType = "ping"
            });
            var requestBytes = System.Text.Encoding.UTF8.GetBytes(requestJson);
            
            _mockProvider.SetNextResult(CloudOperationResult.CreateSuccess(requestBytes));
            var result = await _handler.GetIncomingRequestAsync("truck-001", "req-001");
            
            Assert.True(result.Success);
            Assert.IsType<DeviceRequest>(result.Data);
        }

        [Fact]
        public async Task GetOutgoingResponseAsync_WithValidIds_ReturnsResponse()
        {
            var responseJson = JsonSerializer.Serialize(new DeviceResponse 
            { 
                RequestId = "req-001",
                DeviceId = "truck-001",
                Status = "success"
            });
            var responseBytes = System.Text.Encoding.UTF8.GetBytes(responseJson);
            
            _mockProvider.SetNextResult(CloudOperationResult.CreateSuccess(responseBytes));
            var result = await _handler.GetOutgoingResponseAsync("truck-001", "req-001");
            
            Assert.True(result.Success);
            Assert.IsType<DeviceResponse>(result.Data);
        }

        [Fact]
        public async Task DeleteRequestAsync_WithValidIds_ReturnsSuccess()
        {
            _mockProvider.SetNextResult(CloudOperationResult.CreateSuccess(null));
            var result = await _handler.DeleteRequestAsync("truck-001", "req-001");
            
            Assert.True(result.Success);
        }

        [Fact]
        public async Task ListRequestsAsync_WithValidDeviceId_ReturnsList()
        {
            var files = new List<CloudFile>
            {
                new CloudFile { Id = "1", Name = "req-001.json", Size = 1024, IsFolder = false },
                new CloudFile { Id = "2", Name = "req-002.json", Size = 2048, IsFolder = false }
            };
            
            _mockProvider.SetNextResult(CloudOperationResult.CreateSuccess(files));
            var result = await _handler.ListRequestsAsync("truck-001");
            
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public void RequestHandlerOptions_DefaultValues_AreCorrect()
        {
            var options = new RequestHandlerOptions();
            
            Assert.Equal(3, options.MaxRetries);
            Assert.Equal(100, options.InitialRetryDelayMs);
            Assert.Equal(5000, options.MaxRetryDelayMs);
            Assert.Equal(2.0, options.RetryBackoffMultiplier);
            Assert.Equal(0.1, options.RetryJitterFactor);
        }

        [Fact]
        public void DeviceRequest_Properties_CanBeSet()
        {
            var request = new DeviceRequest
            {
                RequestId = "req-001",
                DeviceId = "truck-001",
                RequestType = "ping",
                Timestamp = DateTime.UtcNow.ToString("o"),
                Version = "1.0"
            };
            
            Assert.Equal("req-001", request.RequestId);
            Assert.Equal("truck-001", request.DeviceId);
            Assert.Equal("ping", request.RequestType);
            Assert.Equal("1.0", request.Version);
        }

        [Fact]
        public void DeviceResponse_Properties_CanBeSet()
        {
            var response = new DeviceResponse
            {
                RequestId = "req-001",
                DeviceId = "truck-001",
                Status = "success",
                ServerBuildSignature = "20251224001",
                Timestamp = DateTime.UtcNow.ToString("o"),
                Version = "1.0"
            };
            
            Assert.Equal("req-001", response.RequestId);
            Assert.Equal("truck-001", response.DeviceId);
            Assert.Equal("success", response.Status);
            Assert.Equal("20251224001", response.ServerBuildSignature);
            Assert.Equal("1.0", response.Version);
        }

        [Fact]
        public void ErrorDetail_Properties_CanBeSet()
        {
            var error = new ErrorDetail
            {
                Code = "invalid_schema",
                Message = "Request validation failed"
            };
            
            Assert.Equal("invalid_schema", error.Code);
            Assert.Equal("Request validation failed", error.Message);
        }
    }

    /// <summary>
    /// Integration tests for RequestHandler with cloud storage.
    /// </summary>
    public class RequestHandlerIntegrationTests
    {
        private readonly MockCloudStorageProvider _mockProvider;
        private readonly RequestHandlerOptions _options;
        private readonly RequestHandler _handler;

        public RequestHandlerIntegrationTests()
        {
            _mockProvider = new MockCloudStorageProvider();
            _options = new RequestHandlerOptions { MaxRetries = 2, InitialRetryDelayMs = 10 };
            _handler = new RequestHandler(_mockProvider, _options);
        }

        [Fact]
        public async Task ProcessIncomingRequest_WithCloudFailure_RetriesAndSucceeds()
        {
            // Fail first time, succeed second time
            _mockProvider.SetFailureCount(1);
            _mockProvider.SetNextResult(CloudOperationResult.CreateSuccess(new { message = "uploaded" }));
            
            var request = new DeviceRequest 
            { 
                RequestId = "req-001", 
                DeviceId = "truck-001",
                RequestType = "ping"
            };
            
            var result = await _handler.ProcessIncomingRequestAsync("truck-001", "req-001", request);
            
            Assert.True(result.Success);
        }

        [Fact]
        public async Task ProcessIncomingRequest_WithPersistentFailure_ReturnsFailure()
        {
            // Always fail
            _mockProvider.SetFailureCount(5);
            
            var request = new DeviceRequest 
            { 
                RequestId = "req-001", 
                DeviceId = "truck-001",
                RequestType = "ping"
            };
            
            var result = await _handler.ProcessIncomingRequestAsync("truck-001", "req-001", request);
            
            Assert.False(result.Success);
            Assert.Contains("failed after", result.Message);
        }

        [Fact]
        public async Task ProcessOutgoingResponse_WithRetry_EventuallySucceeds()
        {
            // Fail first attempt, then succeed
            _mockProvider.SetFailureCount(1);
            _mockProvider.SetNextResult(CloudOperationResult.CreateSuccess(new { message = "uploaded" }));
            
            var response = new DeviceResponse 
            { 
                RequestId = "req-001",
                DeviceId = "truck-001",
                Status = "success"
            };
            
            var result = await _handler.ProcessOutgoingResponseAsync("truck-001", "req-001", response);
            
            Assert.True(result.Success);
        }

        [Fact]
        public async Task MultipleRequests_AllSucceed()
        {
            _mockProvider.SetNextResult(CloudOperationResult.CreateSuccess(new { message = "uploaded" }));
            
            for (int i = 1; i <= 5; i++)
            {
                var request = new DeviceRequest 
                { 
                    RequestId = $"req-{i:D3}", 
                    DeviceId = "truck-001",
                    RequestType = "ping"
                };
                
                var result = await _handler.ProcessIncomingRequestAsync("truck-001", $"req-{i:D3}", request);
                Assert.True(result.Success);
            }
        }

        [Fact]
        public async Task DeleteRequest_WithSuccess_ReturnsSuccess()
        {
            _mockProvider.SetNextResult(CloudOperationResult.CreateSuccess(null));
            var result = await _handler.DeleteRequestAsync("truck-001", "req-001");
            
            Assert.True(result.Success);
        }
    }

    /// <summary>
    /// Mock cloud storage provider for testing.
    /// </summary>
    internal class MockCloudStorageProvider : ICloudStorageProvider
    {
        private CloudOperationResult? _nextResult;
        private int _failureCount = 0;
        private int _callCount = 0;

        public string ProviderName => "Mock";

        public void SetNextResult(CloudOperationResult result) => _nextResult = result;
        public void SetFailureCount(int count) { _failureCount = count; _callCount = 0; }

        public Task<bool> IsAuthenticatedAsync() => Task.FromResult(true);

        public Task<CloudOperationResult> UploadFileAsync(string folderPath, string fileName, byte[] content)
        {
            _callCount++;
            return _callCount <= _failureCount
                ? Task.FromResult(CloudOperationResult.CreateFailure("Mock failure"))
                : Task.FromResult(_nextResult ?? CloudOperationResult.CreateSuccess(new { message = "uploaded" }));
        }

        public Task<CloudOperationResult> UploadFileAsync(string folderPath, string fileName, System.IO.Stream content)
        {
            _callCount++;
            return _callCount <= _failureCount
                ? Task.FromResult(CloudOperationResult.CreateFailure("Mock failure"))
                : Task.FromResult(_nextResult ?? CloudOperationResult.CreateSuccess(new { message = "uploaded" }));
        }

        public Task<CloudOperationResult> DownloadFileAsync(string folderPath, string fileName)
        {
            _callCount++;
            return _callCount <= _failureCount
                ? Task.FromResult(CloudOperationResult.CreateFailure("Mock failure"))
                : Task.FromResult(_nextResult ?? CloudOperationResult.CreateSuccess(new byte[0]));
        }

        public Task<CloudOperationResult> DeleteFileAsync(string folderPath, string fileName)
        {
            _callCount++;
            return _callCount <= _failureCount
                ? Task.FromResult(CloudOperationResult.CreateFailure("Mock failure"))
                : Task.FromResult(_nextResult ?? CloudOperationResult.CreateSuccess(null));
        }

        public Task<CloudOperationResult> ListFilesAsync(string folderPath, string filePattern = "*")
        {
            _callCount++;
            return _callCount <= _failureCount
                ? Task.FromResult(CloudOperationResult.CreateFailure("Mock failure"))
                : Task.FromResult(_nextResult ?? CloudOperationResult.CreateSuccess(new List<CloudFile>()));
        }

        public Task<CloudOperationResult> CreateFolderAsync(string parentPath, string folderName) =>
            Task.FromResult(CloudOperationResult.CreateSuccess(null));

        public Task<CloudOperationResult> MoveFileAsync(string sourcePath, string sourceFileName, string destinationPath, string destinationFileName) =>
            Task.FromResult(CloudOperationResult.CreateSuccess(null));

        public Task<bool> FileExistsAsync(string folderPath, string fileName) => Task.FromResult(true);
        public Task<bool> RefreshAuthenticationAsync() => Task.FromResult(true);
        public Task<CloudOperationResult> GetStorageStatsAsync() => Task.FromResult(CloudOperationResult.CreateSuccess(new { used = 0, total = 0 }));
        public void Dispose() { }
    }
}
