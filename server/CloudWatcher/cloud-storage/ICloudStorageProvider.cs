using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudWatcher.CloudStorage
{
    /// <summary>
    /// Represents a file in cloud storage
    /// </summary>
    public class CloudFile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ContentType { get; set; }
        public bool IsFolder { get; set; }
        public string? ParentId { get; set; }
    }

    /// <summary>
    /// Represents a folder in cloud storage
    /// </summary>
    public class CloudFolder
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ParentId { get; set; }
    }

    /// <summary>
    /// Result of a cloud storage operation
    /// </summary>
    public class CloudOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Exception? Exception { get; set; }
        public object? Data { get; set; }

        public static CloudOperationResult CreateSuccess(object? data = null, string message = "Operation completed successfully")
        {
            return new CloudOperationResult
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static CloudOperationResult CreateFailure(string message, Exception? exception = null)
        {
            return new CloudOperationResult
            {
                Success = false,
                Message = message,
                Exception = exception
            };
        }
    }

    /// <summary>
    /// Abstract interface for cloud storage providers (SharePoint, Google Drive, etc.)
    /// </summary>
    public interface ICloudStorageProvider : IDisposable
    {
        /// <summary>
        /// Provider name (e.g., "SharePoint", "GoogleDrive")
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Indicates if provider is authenticated and ready
        /// </summary>
        Task<bool> IsAuthenticatedAsync();

        /// <summary>
        /// Upload a file to cloud storage
        /// </summary>
        /// <param name="folderPath">Target folder path (e.g., "/Cloud/Requests")</param>
        /// <param name="fileName">File name (e.g., "req-ping-001.json")</param>
        /// <param name="content">File content (bytes)</param>
        /// <returns>Result with file ID on success</returns>
        Task<CloudOperationResult> UploadFileAsync(string folderPath, string fileName, byte[] content);

        /// <summary>
        /// Upload a file from stream (supports large files)
        /// </summary>
        Task<CloudOperationResult> UploadFileAsync(string folderPath, string fileName, System.IO.Stream content);

        /// <summary>
        /// Download a file from cloud storage
        /// </summary>
        /// <param name="folderPath">Folder path (e.g., "/Cloud/Responses/truck-001")</param>
        /// <param name="fileName">File name (e.g., "resp-ping-001.json")</param>
        /// <returns>File content as bytes</returns>
        Task<CloudOperationResult> DownloadFileAsync(string folderPath, string fileName);

        /// <summary>
        /// List files in a folder
        /// </summary>
        /// <param name="folderPath">Folder path to list</param>
        /// <param name="filePattern">Optional file pattern filter (e.g., "*.json")</param>
        /// <returns>List of files in folder</returns>
        Task<CloudOperationResult> ListFilesAsync(string folderPath, string filePattern = "*");

        /// <summary>
        /// Delete a file
        /// </summary>
        /// <param name="folderPath">Folder containing file</param>
        /// <param name="fileName">File to delete</param>
        /// <returns>Success/failure result</returns>
        Task<CloudOperationResult> DeleteFileAsync(string folderPath, string fileName);

        /// <summary>
        /// Create a new folder
        /// </summary>
        /// <param name="parentPath">Parent folder path</param>
        /// <param name="folderName">New folder name</param>
        /// <returns>Result with new folder ID</returns>
        Task<CloudOperationResult> CreateFolderAsync(string parentPath, string folderName);

        /// <summary>
        /// Move or rename a file
        /// </summary>
        /// <param name="sourcePath">Current file path</param>
        /// <param name="sourceFileName">Current file name</param>
        /// <param name="destinationPath">New folder path</param>
        /// <param name="destinationFileName">New file name</param>
        /// <returns>Success/failure result</returns>
        Task<CloudOperationResult> MoveFileAsync(string sourcePath, string sourceFileName, string destinationPath, string destinationFileName);

        /// <summary>
        /// Check if file exists
        /// </summary>
        Task<bool> FileExistsAsync(string folderPath, string fileName);

        /// <summary>
        /// Verify OAuth token is valid, refresh if needed
        /// </summary>
        Task<bool> RefreshAuthenticationAsync();

        /// <summary>
        /// Get provider-specific statistics (storage used, quota, etc.)
        /// </summary>
        Task<CloudOperationResult> GetStorageStatsAsync();
    }
}
