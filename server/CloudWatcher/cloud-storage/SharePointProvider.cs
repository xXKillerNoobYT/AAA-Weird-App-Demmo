using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudWatcher.CloudStorage
{
    /// <summary>
    /// SharePoint cloud storage provider implementation
    /// Uses PnP.Core library for SharePoint/Microsoft 365 access
    /// </summary>
    public class SharePointProvider : ICloudStorageProvider
    {
        private readonly string _siteUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tenantId;
        private readonly HttpClient _httpClient;
        private string _accessToken;
        private DateTime _tokenExpiry;
        private const int TokenRefreshThresholdMinutes = 5;

        public string ProviderName => "SharePoint";

        public SharePointProvider(string siteUrl, string clientId, string clientSecret, string tenantId)
        {
            _siteUrl = siteUrl ?? throw new ArgumentNullException(nameof(siteUrl));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
            _tenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
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
                    Console.WriteLine($"[SharePoint] Authentication token refreshed at {DateTime.UtcNow:O}");
                    return true;
                }

                Console.WriteLine($"[SharePoint] Failed to refresh token: {tokenResult.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SharePoint] Authentication refresh error: {ex.Message}");
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

                // SharePoint REST API: /sites/{site}/drive/root/children
                var uploadUrl = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{NormalizePath(folderPath)}')/Files/add(url='{fileName}',overwrite=true)";

                var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
                {
                    Content = new StreamContent(content)
                };

                request.Headers.Add("Authorization", $"Bearer {_accessToken}");
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[SharePoint] Uploaded file: {folderPath}/{fileName}");
                    return CloudOperationResult.CreateSuccess(fileName, $"File uploaded successfully: {fileName}");
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                return CloudOperationResult.CreateFailure($"Upload failed: {response.StatusCode} - {errorMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SharePoint] Upload error: {ex.Message}");
                return CloudOperationResult.CreateFailure($"Upload error: {ex.Message}", ex);
            }
        }

        public async Task<CloudOperationResult> DownloadFileAsync(string folderPath, string fileName)
        {
            try
            {
                if (!await RefreshAuthenticationAsync())
                    return CloudOperationResult.CreateFailure("Authentication failed");

                var downloadUrl = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{NormalizePath(folderPath)}')/Files('{fileName}')/$value";

                var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    Console.WriteLine($"[SharePoint] Downloaded file: {folderPath}/{fileName}");
                    return CloudOperationResult.CreateSuccess(content, $"File downloaded: {fileName}");
                }

                return CloudOperationResult.CreateFailure($"Download failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SharePoint] Download error: {ex.Message}");
                return CloudOperationResult.CreateFailure($"Download error: {ex.Message}", ex);
            }
        }

        public async Task<CloudOperationResult> ListFilesAsync(string folderPath, string filePattern = "*")
        {
            try
            {
                if (!await RefreshAuthenticationAsync())
                    return CloudOperationResult.CreateFailure("Authentication failed");

                var listUrl = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{NormalizePath(folderPath)}')/Files";

                var request = new HttpRequestMessage(HttpMethod.Get, listUrl);
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(content);
                    var files = new List<CloudFile>();

                    foreach (var item in jsonDoc.RootElement.GetProperty("value").EnumerateArray())
                    {
                        var fileName = item.GetProperty("Name").GetString();

                        // Filter by pattern if specified
                        if (filePattern != "*" && !MatchesPattern(fileName, filePattern))
                            continue;

                        files.Add(new CloudFile
                        {
                            Id = item.GetProperty("UniqueId").GetString(),
                            Name = fileName,
                            Size = item.GetProperty("Length").GetInt64(),
                            ModifiedDate = DateTime.Parse(item.GetProperty("TimeLastModified").GetString()),
                            IsFolder = false
                        });
                    }

                    Console.WriteLine($"[SharePoint] Listed {files.Count} files in {folderPath}");
                    return CloudOperationResult.CreateSuccess(files, $"Found {files.Count} files");
                }

                return CloudOperationResult.CreateFailure($"List failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SharePoint] List error: {ex.Message}");
                return CloudOperationResult.CreateFailure($"List error: {ex.Message}", ex);
            }
        }

        public async Task<CloudOperationResult> DeleteFileAsync(string folderPath, string fileName)
        {
            try
            {
                if (!await RefreshAuthenticationAsync())
                    return CloudOperationResult.CreateFailure("Authentication failed");

                var deleteUrl = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{NormalizePath(folderPath)}')/Files('{fileName}')";

                var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");
                request.Headers.Add("X-HTTP-Method", "DELETE");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    Console.WriteLine($"[SharePoint] Deleted file: {folderPath}/{fileName}");
                    return CloudOperationResult.CreateSuccess(null, $"File deleted: {fileName}");
                }

                return CloudOperationResult.CreateFailure($"Delete failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SharePoint] Delete error: {ex.Message}");
                return CloudOperationResult.CreateFailure($"Delete error: {ex.Message}", ex);
            }
        }

        public async Task<CloudOperationResult> CreateFolderAsync(string parentPath, string folderName)
        {
            try
            {
                if (!await RefreshAuthenticationAsync())
                    return CloudOperationResult.CreateFailure("Authentication failed");

                var createUrl = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{NormalizePath(parentPath)}')/Folders";

                var payload = new { __metadata = new { type = "SP.Folder" }, ServerRelativeUrl = $"{NormalizePath(parentPath)}/{folderName}" };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, createUrl) { Content = content };
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[SharePoint] Created folder: {parentPath}/{folderName}");
                    return CloudOperationResult.CreateSuccess(folderName, $"Folder created: {folderName}");
                }

                return CloudOperationResult.CreateFailure($"Create folder failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SharePoint] Create folder error: {ex.Message}");
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

                Console.WriteLine($"[SharePoint] Moved file: {sourcePath}/{sourceFileName} → {destinationPath}/{destinationFileName}");
                return CloudOperationResult.CreateSuccess(null, "File moved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SharePoint] Move error: {ex.Message}");
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

                var statsUrl = $"{_siteUrl}/_api/site/usage";

                var request = new HttpRequestMessage(HttpMethod.Get, statsUrl);
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");
                request.Headers.Add("Accept", "application/json");

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

        private async Task<CloudOperationResult> GetAccessTokenAsync()
        {
            try
            {
                var tokenEndpoint = $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token";
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                    new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
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

        private string NormalizePath(string path)
        {
            return path.TrimStart('/').Replace("\\", "/");
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
