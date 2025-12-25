using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudWatcher.CloudStorage
{
    /// <summary>
    /// Google Drive cloud storage provider implementation
    /// Uses Google.Apis.Drive.v3 for Google Drive access
    /// </summary>
    public class GoogleDriveProvider : ICloudStorageProvider
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _refreshToken;
        private readonly HttpClient _httpClient;
        private string _accessToken;
        private DateTime _tokenExpiry;
        private const int TokenRefreshThresholdMinutes = 5;

        public string ProviderName => "GoogleDrive";

        public GoogleDriveProvider(string clientId, string clientSecret, string refreshToken)
        {
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
            _refreshToken = refreshToken ?? throw new ArgumentNullException(nameof(refreshToken));
            _httpClient = new HttpClient();
            _tokenExpiry = DateTime.UtcNow;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                return await RefreshAuthenticationAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RefreshAuthenticationAsync()
        {
            try
            {
                // If token still valid for >5 minutes, don't refresh
                if (DateTime.UtcNow < _tokenExpiry.AddMinutes(-TokenRefreshThresholdMinutes))
                {
                    return true;
                }

                var tokenResult = await GetAccessTokenAsync();
                if (tokenResult.Success)
                {
                    _accessToken = tokenResult.Data?.ToString();
                    _tokenExpiry = DateTime.UtcNow.AddHours(1); // Tokens valid for 1 hour
                    Console.WriteLine($"[GoogleDrive] Authentication token refreshed at {DateTime.UtcNow:O}");
                    return true;
                }

                Console.WriteLine($"[GoogleDrive] Failed to refresh token: {tokenResult.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleDrive] Authentication refresh error: {ex.Message}");
                return false;
            }
        }

        public async Task<CloudOperationResult> UploadFileAsync(string folderPath, string fileName, byte[] content)
        {
            using (var stream = new MemoryStream(content))
            {
                return await UploadFileAsync(folderPath, fileName, stream);
            }
        }

        public async Task<CloudOperationResult> UploadFileAsync(string folderPath, string fileName, Stream content)
        {
            try
            {
                if (!await RefreshAuthenticationAsync())
                    return CloudOperationResult.CreateFailure("Authentication failed");

                // Find or create folder
                var folderId = await GetOrCreateFolderIdAsync(folderPath);
                if (string.IsNullOrEmpty(folderId))
                    return CloudOperationResult.CreateFailure("Failed to get/create folder");

                // Create file metadata
                var fileMetadata = new
                {
                    name = fileName,
                    parents = new[] { folderId }
                };

                var json = JsonSerializer.Serialize(fileMetadata);
                var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

                // Multipart upload
                var multipartContent = new MultipartFormDataContent();
                multipartContent.Add(stringContent, "metadata");
                multipartContent.Add(new StreamContent(content), "file");

                var uploadUrl = "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart";
                var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl) { Content = multipartContent };
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GoogleDrive] Uploaded file: {folderPath}/{fileName}");
                    return CloudOperationResult.CreateSuccess(fileName, $"File uploaded: {fileName}");
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                return CloudOperationResult.CreateFailure($"Upload failed: {response.StatusCode} - {errorMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleDrive] Upload error: {ex.Message}");
                return CloudOperationResult.CreateFailure($"Upload error: {ex.Message}", ex);
            }
        }

        public async Task<CloudOperationResult> DownloadFileAsync(string folderPath, string fileName)
        {
            try
            {
                if (!await RefreshAuthenticationAsync())
                    return CloudOperationResult.CreateFailure("Authentication failed");

                var fileId = await GetFileIdAsync(folderPath, fileName);
                if (string.IsNullOrEmpty(fileId))
                    return CloudOperationResult.CreateFailure($"File not found: {folderPath}/{fileName}");

                var downloadUrl = $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media";
                var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    Console.WriteLine($"[GoogleDrive] Downloaded file: {folderPath}/{fileName}");
                    return CloudOperationResult.CreateSuccess(content, $"File downloaded: {fileName}");
                }

                return CloudOperationResult.CreateFailure($"Download failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleDrive] Download error: {ex.Message}");
                return CloudOperationResult.CreateFailure($"Download error: {ex.Message}", ex);
            }
        }

        public async Task<CloudOperationResult> ListFilesAsync(string folderPath, string filePattern = "*")
        {
            try
            {
                if (!await RefreshAuthenticationAsync())
                    return CloudOperationResult.CreateFailure("Authentication failed");

                var folderId = await GetOrCreateFolderIdAsync(folderPath);
                if (string.IsNullOrEmpty(folderId))
                    return CloudOperationResult.CreateFailure("Failed to get/create folder");

                var listUrl = $"https://www.googleapis.com/drive/v3/files?q='{folderId}'+in+parents+and+trashed=false&fields=files(id,name,size,modifiedTime)";
                var request = new HttpRequestMessage(HttpMethod.Get, listUrl);
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(content);
                    var files = new List<CloudFile>();

                    foreach (var item in jsonDoc.RootElement.GetProperty("files").EnumerateArray())
                    {
                        var fileName = item.GetProperty("name").GetString();

                        // Filter by pattern if specified
                        if (filePattern != "*" && !MatchesPattern(fileName, filePattern))
                            continue;

                        files.Add(new CloudFile
                        {
                            Id = item.GetProperty("id").GetString(),
                            Name = fileName,
                            Size = item.TryGetProperty("size", out var sizeElement) ? sizeElement.GetInt64() : 0,
                            ModifiedDate = DateTime.Parse(item.GetProperty("modifiedTime").GetString()),
                            IsFolder = false
                        });
                    }

                    Console.WriteLine($"[GoogleDrive] Listed {files.Count} files in {folderPath}");
                    return CloudOperationResult.CreateSuccess(files, $"Found {files.Count} files");
                }

                return CloudOperationResult.CreateFailure($"List failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleDrive] List error: {ex.Message}");
                return CloudOperationResult.CreateFailure($"List error: {ex.Message}", ex);
            }
        }

        public async Task<CloudOperationResult> DeleteFileAsync(string folderPath, string fileName)
        {
            try
            {
                if (!await RefreshAuthenticationAsync())
                    return CloudOperationResult.CreateFailure("Authentication failed");

                var fileId = await GetFileIdAsync(folderPath, fileName);
                if (string.IsNullOrEmpty(fileId))
                    return CloudOperationResult.CreateFailure($"File not found: {folderPath}/{fileName}");

                var deleteUrl = $"https://www.googleapis.com/drive/v3/files/{fileId}";
                var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    Console.WriteLine($"[GoogleDrive] Deleted file: {folderPath}/{fileName}");
                    return CloudOperationResult.CreateSuccess(null, $"File deleted: {fileName}");
                }

                return CloudOperationResult.CreateFailure($"Delete failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleDrive] Delete error: {ex.Message}");
                return CloudOperationResult.CreateFailure($"Delete error: {ex.Message}", ex);
            }
        }

        public async Task<CloudOperationResult> CreateFolderAsync(string parentPath, string folderName)
        {
            try
            {
                if (!await RefreshAuthenticationAsync())
                    return CloudOperationResult.CreateFailure("Authentication failed");

                var parentId = await GetOrCreateFolderIdAsync(parentPath);
                if (string.IsNullOrEmpty(parentId))
                    return CloudOperationResult.CreateFailure("Failed to get parent folder");

                var metadata = new
                {
                    name = folderName,
                    mimeType = "application/vnd.google-apps.folder",
                    parents = new[] { parentId }
                };

                var content = new StringContent(JsonSerializer.Serialize(metadata), Encoding.UTF8, "application/json");
                var createUrl = "https://www.googleapis.com/drive/v3/files?fields=id";
                var request = new HttpRequestMessage(HttpMethod.Post, createUrl) { Content = content };
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[GoogleDrive] Created folder: {parentPath}/{folderName}");
                    return CloudOperationResult.CreateSuccess(folderName, $"Folder created: {folderName}");
                }

                return CloudOperationResult.CreateFailure($"Create folder failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleDrive] Create folder error: {ex.Message}");
                return CloudOperationResult.CreateFailure($"Create folder error: {ex.Message}", ex);
            }
        }

        public async Task<CloudOperationResult> MoveFileAsync(string sourcePath, string sourceFileName, string destinationPath, string destinationFileName)
        {
            try
            {
                // Download file
                var downloadResult = await DownloadFileAsync(sourcePath, sourceFileName);
                if (!downloadResult.Success)
                    return downloadResult;

                // Upload to new location
                var uploadResult = await UploadFileAsync(destinationPath, destinationFileName, (byte[])downloadResult.Data);
                if (!uploadResult.Success)
                    return uploadResult;

                // Delete original
                await DeleteFileAsync(sourcePath, sourceFileName);

                Console.WriteLine($"[GoogleDrive] Moved file: {sourcePath}/{sourceFileName} → {destinationPath}/{destinationFileName}");
                return CloudOperationResult.CreateSuccess(null, "File moved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleDrive] Move error: {ex.Message}");
                return CloudOperationResult.CreateFailure($"Move error: {ex.Message}", ex);
            }
        }

        public async Task<bool> FileExistsAsync(string folderPath, string fileName)
        {
            try
            {
                var result = await ListFilesAsync(folderPath);
                if (!result.Success)
                    return false;

                var files = (List<CloudFile>)result.Data;
                return files.Any(f => f.Name == fileName);
            }
            catch
            {
                return false;
            }
        }

        public async Task<CloudOperationResult> GetStorageStatsAsync()
        {
            try
            {
                if (!await RefreshAuthenticationAsync())
                    return CloudOperationResult.CreateFailure("Authentication failed");

                var aboutUrl = "https://www.googleapis.com/drive/v3/about?fields=storageQuota";
                var request = new HttpRequestMessage(HttpMethod.Get, aboutUrl);
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return CloudOperationResult.CreateSuccess(content, "Storage stats retrieved");
                }

                return CloudOperationResult.CreateFailure($"Stats failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return CloudOperationResult.CreateFailure($"Stats error: {ex.Message}", ex);
            }
        }

        private async Task<string> GetOrCreateFolderIdAsync(string folderPath)
        {
            var parts = folderPath.Trim('/').Split('/');
            var currentParentId = "root";

            foreach (var part in parts)
            {
                var folderId = await FindFolderIdAsync(currentParentId, part);
                if (string.IsNullOrEmpty(folderId))
                {
                    // Create folder if it doesn't exist
                    var createResult = await CreateFolderInternalAsync(currentParentId, part);
                    if (!createResult.Success)
                        return null;
                    folderId = createResult.Data?.ToString();
                }
                currentParentId = folderId;
            }

            return currentParentId;
        }

        private async Task<string> FindFolderIdAsync(string parentId, string folderName)
        {
            try
            {
                var query = $"'{parentId}'+in+parents+and+name='{folderName}'+and+mimeType='application/vnd.google-apps.folder'+and+trashed=false";
                var searchUrl = $"https://www.googleapis.com/drive/v3/files?q={Uri.EscapeDataString(query)}&fields=files(id)";
                var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(content);
                    var files = jsonDoc.RootElement.GetProperty("files");
                    if (files.GetArrayLength() > 0)
                    {
                        return files[0].GetProperty("id").GetString();
                    }
                }
            }
            catch { }

            return null;
        }

        private async Task<string> GetFileIdAsync(string folderPath, string fileName)
        {
            var folderId = await GetOrCreateFolderIdAsync(folderPath);
            if (string.IsNullOrEmpty(folderId))
                return null;

            try
            {
                var query = $"'{folderId}'+in+parents+and+name='{fileName}'+and+trashed=false";
                var searchUrl = $"https://www.googleapis.com/drive/v3/files?q={Uri.EscapeDataString(query)}&fields=files(id)";
                var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(content);
                    var files = jsonDoc.RootElement.GetProperty("files");
                    if (files.GetArrayLength() > 0)
                    {
                        return files[0].GetProperty("id").GetString();
                    }
                }
            }
            catch { }

            return null;
        }

        private async Task<CloudOperationResult> CreateFolderInternalAsync(string parentId, string folderName)
        {
            try
            {
                var metadata = new
                {
                    name = folderName,
                    mimeType = "application/vnd.google-apps.folder",
                    parents = new[] { parentId }
                };

                var content = new StringContent(JsonSerializer.Serialize(metadata), Encoding.UTF8, "application/json");
                var createUrl = "https://www.googleapis.com/drive/v3/files?fields=id";
                var request = new HttpRequestMessage(HttpMethod.Post, createUrl) { Content = content };
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var folderId = jsonDoc.RootElement.GetProperty("id").GetString();
                    return CloudOperationResult.CreateSuccess(folderId);
                }

                return CloudOperationResult.CreateFailure($"Create folder failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return CloudOperationResult.CreateFailure($"Create folder error: {ex.Message}", ex);
            }
        }

        private async Task<CloudOperationResult> GetAccessTokenAsync()
        {
            try
            {
                var tokenEndpoint = "https://oauth2.googleapis.com/token";
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                    new KeyValuePair<string, string>("refresh_token", _refreshToken),
                    new KeyValuePair<string, string>("grant_type", "refresh_token")
                });

                var response = await _httpClient.PostAsync(tokenEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenData = JsonDocument.Parse(responseContent);
                    var token = tokenData.RootElement.GetProperty("access_token").GetString();
                    return CloudOperationResult.CreateSuccess(token);
                }

                return CloudOperationResult.CreateFailure($"Token request failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return CloudOperationResult.CreateFailure($"Token request error: {ex.Message}", ex);
            }
        }

        private bool MatchesPattern(string fileName, string pattern)
        {
            if (pattern == "*")
                return true;
            if (pattern.StartsWith("*."))
            {
                var extension = pattern.Substring(1);
                return fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
            }
            return fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
