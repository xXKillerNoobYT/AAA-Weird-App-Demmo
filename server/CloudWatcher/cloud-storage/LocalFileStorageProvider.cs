using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CloudWatcher.CloudStorage
{
    /// <summary>
    /// Local file system implementation of ICloudStorageProvider.
    /// Stores files directly on disk without cloud authentication.
    /// Useful for local testing and development.
    /// </summary>
    public class LocalFileStorageProvider : ICloudStorageProvider
    {
        private readonly string _baseRoot;
        public string ProviderName => "LocalFile";

        public LocalFileStorageProvider()
        {
            // Use the Cloud folder at repo root
            var repoRoot = Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.FullName;
            _baseRoot = Path.Combine(repoRoot, "Cloud");
            Directory.CreateDirectory(_baseRoot);
        }

        public Task<bool> IsAuthenticatedAsync() => Task.FromResult(true);

        public async Task<CloudOperationResult> UploadFileAsync(string folderPath, string fileName, byte[] content)
        {
            try
            {
                var fullPath = Path.Combine(_baseRoot, folderPath.TrimStart('/'), fileName);
                var directory = Path.GetDirectoryName(fullPath)!;
                Directory.CreateDirectory(directory);

                // Write to temp file first, then move atomically
                var tempPath = fullPath + ".tmp";
                await File.WriteAllBytesAsync(tempPath, content);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
                File.Move(tempPath, fullPath);

                return CloudOperationResult.CreateSuccess(new { path = fullPath, size = content.Length });
            }
            catch (Exception ex)
            {
                return CloudOperationResult.CreateFailure($"Failed to upload file: {ex.Message}", ex);
            }
        }

        public async Task<CloudOperationResult> UploadFileAsync(string folderPath, string fileName, Stream content)
        {
            try
            {
                var fullPath = Path.Combine(_baseRoot, folderPath.TrimStart('/'), fileName);
                var directory = Path.GetDirectoryName(fullPath)!;
                Directory.CreateDirectory(directory);

                // Write to temp file first, then move atomically
                var tempPath = fullPath + ".tmp";
                using (var fileStream = File.Create(tempPath))
                {
                    await content.CopyToAsync(fileStream);
                }

                if (File.Exists(fullPath))
                    File.Delete(fullPath);
                File.Move(tempPath, fullPath);

                var fileInfo = new FileInfo(fullPath);
                return CloudOperationResult.CreateSuccess(new { path = fullPath, size = fileInfo.Length });
            }
            catch (Exception ex)
            {
                return CloudOperationResult.CreateFailure($"Failed to upload file from stream: {ex.Message}", ex);
            }
        }

        public async Task<CloudOperationResult> DownloadFileAsync(string folderPath, string fileName)
        {
            try
            {
                var fullPath = Path.Combine(_baseRoot, folderPath.TrimStart('/'), fileName);
                if (!File.Exists(fullPath))
                    return CloudOperationResult.CreateFailure($"File not found: {fullPath}");

                var content = await File.ReadAllBytesAsync(fullPath);
                return CloudOperationResult.CreateSuccess(content);
            }
            catch (Exception ex)
            {
                return CloudOperationResult.CreateFailure($"Failed to download file: {ex.Message}", ex);
            }
        }

        public Task<CloudOperationResult> DeleteFileAsync(string folderPath, string fileName)
        {
            try
            {
                var fullPath = Path.Combine(_baseRoot, folderPath.TrimStart('/'), fileName);
                if (!File.Exists(fullPath))
                    return Task.FromResult(CloudOperationResult.CreateFailure($"File not found: {fullPath}"));

                File.Delete(fullPath);
                return Task.FromResult(CloudOperationResult.CreateSuccess(null));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CloudOperationResult.CreateFailure($"Failed to delete file: {ex.Message}", ex));
            }
        }

        public Task<CloudOperationResult> ListFilesAsync(string folderPath, string filePattern = "*")
        {
            try
            {
                var fullPath = Path.Combine(_baseRoot, folderPath.TrimStart('/'));
                if (!Directory.Exists(fullPath))
                    return Task.FromResult(CloudOperationResult.CreateSuccess(new List<CloudFile>()));

                var files = new List<CloudFile>();
                foreach (var filePath in Directory.EnumerateFiles(fullPath, filePattern))
                {
                    var fileInfo = new FileInfo(filePath);
                    files.Add(new CloudFile
                    {
                        Id = fileInfo.FullName,
                        Name = fileInfo.Name,
                        ContentType = "application/json",
                        ParentId = fullPath
                    });
                }

                return Task.FromResult(CloudOperationResult.CreateSuccess(files));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CloudOperationResult.CreateFailure($"Failed to list files: {ex.Message}", ex));
            }
        }

        public Task<CloudOperationResult> CreateFolderAsync(string parentPath, string folderName)
        {
            try
            {
                var fullPath = Path.Combine(_baseRoot, parentPath.TrimStart('/'), folderName);
                Directory.CreateDirectory(fullPath);
                return Task.FromResult(CloudOperationResult.CreateSuccess(new { path = fullPath }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CloudOperationResult.CreateFailure($"Failed to create folder: {ex.Message}", ex));
            }
        }

        public Task<CloudOperationResult> MoveFileAsync(string sourcePath, string sourceFileName, 
            string destinationPath, string destinationFileName)
        {
            try
            {
                var sourceFullPath = Path.Combine(_baseRoot, sourcePath.TrimStart('/'), sourceFileName);
                var destFullPath = Path.Combine(_baseRoot, destinationPath.TrimStart('/'), destinationFileName);

                if (!File.Exists(sourceFullPath))
                    return Task.FromResult(CloudOperationResult.CreateFailure($"Source file not found: {sourceFullPath}"));

                var destDir = Path.GetDirectoryName(destFullPath)!;
                Directory.CreateDirectory(destDir);

                if (File.Exists(destFullPath))
                    File.Delete(destFullPath);

                File.Move(sourceFullPath, destFullPath);
                return Task.FromResult(CloudOperationResult.CreateSuccess(new { path = destFullPath }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CloudOperationResult.CreateFailure($"Failed to move file: {ex.Message}", ex));
            }
        }

        public Task<bool> FileExistsAsync(string folderPath, string fileName)
        {
            try
            {
                var fullPath = Path.Combine(_baseRoot, folderPath.TrimStart('/'), fileName);
                return Task.FromResult(File.Exists(fullPath));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> RefreshAuthenticationAsync() => Task.FromResult(true);

        public Task<CloudOperationResult> GetStorageStatsAsync()
        {
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(_baseRoot)!);
                return Task.FromResult(CloudOperationResult.CreateSuccess(new
                {
                    used = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
                    total = driveInfo.TotalSize,
                    available = driveInfo.AvailableFreeSpace
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CloudOperationResult.CreateFailure($"Failed to get storage stats: {ex.Message}", ex));
            }
        }

        public void Dispose()
        {
            // No resources to clean up for local file storage
        }
    }
}
