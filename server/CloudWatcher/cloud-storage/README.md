# Cloud Storage Provider Implementation

## Overview

This is a production-ready cloud storage abstraction layer implemented in C# that provides a unified interface for multiple cloud storage providers:

- **SharePoint** (Microsoft 365/OneDrive for Business)
- **Google Drive**

The implementation uses the factory pattern for provider instantiation and provides comprehensive error handling, logging, and testing.

## Architecture

### Core Components

#### 1. **ICloudStorageProvider** Interface
Defines the contract that all cloud storage providers must implement:

```csharp
public interface ICloudStorageProvider : IDisposable
{
    string ProviderName { get; }
    Task<bool> IsAuthenticatedAsync();
    Task<CloudOperationResult> UploadFileAsync(string folderPath, string fileName, byte[] content);
    Task<CloudOperationResult> DownloadFileAsync(string folderPath, string fileName);
    Task<CloudOperationResult> ListFilesAsync(string folderPath, string filePattern = "*");
    Task<CloudOperationResult> DeleteFileAsync(string folderPath, string fileName);
    Task<CloudOperationResult> CreateFolderAsync(string parentPath, string folderName);
    Task<CloudOperationResult> MoveFileAsync(string sourcePath, string sourceFileName, 
                                            string destinationPath, string destinationFileName);
    Task<bool> FileExistsAsync(string folderPath, string fileName);
    Task<bool> RefreshAuthenticationAsync();
    Task<CloudOperationResult> GetStorageStatsAsync();
}
```

#### 2. **SharePointProvider**
Implements cloud storage for SharePoint/OneDrive using OAuth2:

**Key Features:**
- Uses Azure AD OAuth2 for authentication
- SharePoint REST API integration
- Automatic token refresh with 5-minute threshold
- Support for folder creation and file operations
- Comprehensive error handling with detailed messages

**Configuration Required:**
```json
{
    "siteUrl": "https://company.sharepoint.com/sites/project",
    "clientId": "azure-app-id",
    "clientSecret": "azure-app-secret",
    "tenantId": "azure-tenant-id"
}
```

#### 3. **GoogleDriveProvider**
Implements cloud storage for Google Drive using OAuth2:

**Key Features:**
- Google OAuth2 refresh token authentication
- Google Drive API v3 integration
- Automatic folder creation if needed
- Support for nested folder structures
- File upload with multipart support

**Configuration Required:**
```json
{
    "clientId": "google-client-id",
    "clientSecret": "google-client-secret",
    "refreshToken": "google-refresh-token"
}
```

#### 4. **CloudStorageFactory**
Factory class for creating provider instances:

```csharp
// Create by enum
var provider = CloudStorageFactory.CreateProvider(
    CloudStorageFactory.ProviderType.SharePoint,
    config
);

// Create by string name (case-insensitive)
var provider = CloudStorageFactory.CreateProvider("sharepoint", config);
var provider = CloudStorageFactory.CreateProvider("googledrive", config);
```

#### 5. **CloudOperationResult**
Wraps operation results with success/failure status:

```csharp
public class CloudOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public Exception? Exception { get; set; }
    public object? Data { get; set; }
}
```

#### 6. **CloudFile & CloudFolder**
Models representing cloud resources:

```csharp
public class CloudFile
{
    public string Id { get; set; }
    public string Name { get; set; }
    public long Size { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string ContentType { get; set; }
    public bool IsFolder { get; set; }
}

public class CloudFolder
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public string ParentId { get; set; }
}
```

## Usage Examples

### Upload a File

```csharp
// Create provider
var config = new Dictionary<string, string>
{
    { "siteUrl", "https://company.sharepoint.com/sites/project" },
    { "clientId", "app-id" },
    { "clientSecret", "app-secret" },
    { "tenantId", "tenant-id" }
};

using (var provider = CloudStorageFactory.CreateProvider(
    CloudStorageFactory.ProviderType.SharePoint, config))
{
    // Upload file
    var fileContent = File.ReadAllBytes("document.pdf");
    var result = await provider.UploadFileAsync(
        "/Cloud/Requests/truck-001",
        "req-ping-001.json",
        fileContent
    );

    if (result.Success)
    {
        Console.WriteLine($"File uploaded: {result.Message}");
    }
    else
    {
        Console.WriteLine($"Upload failed: {result.Message}");
        if (result.Exception != null)
        {
            Console.WriteLine($"Error: {result.Exception.Message}");
        }
    }
}
```

### List Files with Pattern

```csharp
var result = await provider.ListFilesAsync(
    "/Cloud/Requests",
    "*.json"  // Filter by pattern
);

if (result.Success)
{
    var files = (List<CloudFile>)result.Data;
    foreach (var file in files)
    {
        Console.WriteLine($"File: {file.Name} ({file.Size} bytes)");
        Console.WriteLine($"Modified: {file.ModifiedDate:O}");
    }
}
```

### Download a File

```csharp
var result = await provider.DownloadFileAsync(
    "/Cloud/Responses/truck-001",
    "resp-ping-001.json"
);

if (result.Success)
{
    var fileContent = (byte[])result.Data;
    File.WriteAllBytes("response.json", fileContent);
}
```

### Move/Rename a File

```csharp
var result = await provider.MoveFileAsync(
    "/Cloud/Requests",
    "req-ping-001.json",
    "/Cloud/Responses",
    "resp-ping-001.json"
);

if (result.Success)
{
    Console.WriteLine("File moved successfully");
}
```

### Check Storage Statistics

```csharp
var result = await provider.GetStorageStatsAsync();
if (result.Success)
{
    Console.WriteLine($"Storage stats: {result.Data}");
}
```

## Authentication

### SharePoint OAuth2 Flow

1. **Get Azure App Registration:**
   - Register app in Azure AD
   - Generate client ID and secret
   - Grant permissions: `SharePoint.AllTenancy.FullControl` or `Sites.FullControl.All`

2. **Token Management:**
   - Tokens are cached until 5 minutes before expiry
   - Automatic refresh happens on `RefreshAuthenticationAsync()`
   - Each operation checks and refreshes token if needed

### Google Drive OAuth2 Flow

1. **Get Google Credentials:**
   - Create OAuth 2.0 credentials in Google Cloud Console
   - Generate refresh token (requires user consent once)
   - Store refresh token securely

2. **Token Management:**
   - Tokens are cached until 5 minutes before expiry
   - Automatic refresh happens on `RefreshAuthenticationAsync()`

## Error Handling

All operations return `CloudOperationResult` with:
- **Success**: Boolean indicating operation status
- **Message**: Human-readable message
- **Exception**: Underlying exception if any (null on success)
- **Data**: Operation result data (files list, file content, etc.)

**Common Errors:**
- Authentication failures (invalid credentials)
- Network errors (HTTP failures)
- File not found errors
- Permission denied errors
- Storage quota exceeded

**Best Practices:**
```csharp
var result = await provider.UploadFileAsync(path, fileName, content);

if (!result.Success)
{
    // Log error details
    Console.WriteLine($"Operation failed: {result.Message}");
    if (result.Exception != null)
    {
        Console.WriteLine($"Exception: {result.Exception}");
        // Handle specific exception types
        if (result.Exception is HttpRequestException)
        {
            // Handle network errors
        }
    }
}
```

## Testing

The implementation includes comprehensive unit and integration tests:

### Unit Tests (CloudStorageProviderTests.cs)
- Factory creation with valid/invalid configurations
- Result object creation
- Model validation

### Integration Tests (CloudStorageIntegrationTests.cs)
- Authentication with invalid credentials
- File operations with mock credentials
- Pattern matching for file listings
- Case-insensitive provider names

**Run Tests:**
```bash
dotnet test --configuration Release
```

**Test Results:**
```
Test summary: total: 26, failed: 0, succeeded: 26, skipped: 0
```

## Performance Considerations

1. **Token Caching:**
   - Tokens cached for ~1 hour
   - Automatic refresh when within 5 minutes of expiry
   - Reduces unnecessary token requests

2. **Folder Navigation:**
   - SharePoint: Direct API calls per folder level
   - Google Drive: Creates folders if they don't exist

3. **File Operations:**
   - Stream support for large files
   - Multipart upload for Google Drive
   - Chunked reading for downloads

## Security Considerations

1. **Credential Storage:**
   - Never hardcode credentials in code
   - Use environment variables or secure configuration
   - Credentials should be encrypted at rest

2. **Token Handling:**
   - Tokens are sensitive; handle carefully
   - Never log full tokens (only first 10 chars if logging)
   - Refresh tokens stored securely

3. **HTTPS Only:**
   - All API calls use HTTPS
   - TLS validation enabled by default

4. **Scope Limitation:**
   - Configure minimal required permissions
   - SharePoint: Site-level permissions
   - Google Drive: Drive scope limited to specific folders

## Deployment

### Environment Variables

Set before running:
```bash
# SharePoint
export SHAREPOINT_SITE_URL="https://company.sharepoint.com/sites/project"
export SHAREPOINT_CLIENT_ID="app-id"
export SHAREPOINT_CLIENT_SECRET="app-secret"
export SHAREPOINT_TENANT_ID="tenant-id"

# Google Drive
export GOOGLE_CLIENT_ID="client-id"
export GOOGLE_CLIENT_SECRET="client-secret"
export GOOGLE_REFRESH_TOKEN="refresh-token"
```

### Configuration from Environment

```csharp
var spConfig = new Dictionary<string, string>
{
    { "siteUrl", Environment.GetEnvironmentVariable("SHAREPOINT_SITE_URL") },
    { "clientId", Environment.GetEnvironmentVariable("SHAREPOINT_CLIENT_ID") },
    { "clientSecret", Environment.GetEnvironmentVariable("SHAREPOINT_CLIENT_SECRET") },
    { "tenantId", Environment.GetEnvironmentVariable("SHAREPOINT_TENANT_ID") }
};

var provider = CloudStorageFactory.CreateProvider("sharepoint", spConfig);
```

## Troubleshooting

### Authentication Failures

**Issue:** "Token request failed: Unauthorized"
- **Cause:** Invalid credentials or expired refresh token
- **Solution:** Verify credentials are correct, refresh Google token if needed

**Issue:** "Token request failed: BadRequest"
- **Cause:** Invalid Azure AD configuration
- **Solution:** Verify tenant ID, client ID, and site URL are correct

### File Operations Failures

**Issue:** "File not found"
- **Cause:** Path or filename doesn't exist
- **Solution:** Verify path exists using `ListFilesAsync()` first

**Issue:** "Access denied"
- **Cause:** Insufficient permissions
- **Solution:** Check OAuth2 scopes and permissions in Azure AD or Google Cloud

### Network Issues

**Issue:** "Unable to connect to remote server"
- **Cause:** Network connectivity or firewall blocking
- **Solution:** Check network connectivity and firewall rules

## Future Enhancements

1. **Additional Providers:**
   - OneDrive personal
   - Dropbox
   - Amazon S3
   - Azure Blob Storage

2. **Features:**
   - Batch operations
   - Resume capability for large uploads
   - Retry policies with exponential backoff
   - Server-side copy operations
   - Async generators for streaming large file lists

3. **Performance:**
   - Connection pooling
   - Request caching
   - Parallel upload/download

## File Structure

```
cloud-storage/
├── ICloudStorageProvider.cs        # Interface definitions
├── SharePointProvider.cs           # SharePoint implementation
├── GoogleDriveProvider.cs          # Google Drive implementation
├── CloudStorageFactory.cs          # Factory pattern implementation
└── Tests/
    ├── CloudStorageProviderTests.cs       # Unit tests
    └── CloudStorageIntegrationTests.cs    # Integration tests
```

## License

This implementation is part of the CloudWatcher project.

## Support

For issues or questions:
1. Check test cases for usage examples
2. Review error messages and exceptions
3. Verify credentials and permissions
4. Check documentation for specific provider requirements
