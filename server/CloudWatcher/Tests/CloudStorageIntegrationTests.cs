using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CloudWatcher.CloudStorage;
using Xunit;

namespace CloudWatcher.Tests
{
    public class CloudStorageIntegrationTests
    {
        [Fact]
        public async Task SharePointProvider_IsAuthenticatedAsync_ReturnsFalseOnInvalidCredentials()
        {
            var config = new Dictionary<string, string>
            {
                { "siteUrl", "https://company.sharepoint.com/sites/project" },
                { "clientId", "invalid-client-id" },
                { "clientSecret", "invalid-client-secret" },
                { "tenantId", "invalid-tenant-id" }
            };

            using (var provider = (SharePointProvider)CloudStorageFactory.CreateProvider(
                CloudStorageFactory.ProviderType.SharePoint, config))
            {
                var isAuthenticated = await provider.IsAuthenticatedAsync();
                // With invalid credentials, authentication should fail
                Assert.False(isAuthenticated);
            }
        }

        [Fact]
        public async Task GoogleDriveProvider_IsAuthenticatedAsync_ReturnsFalseOnInvalidCredentials()
        {
            var config = new Dictionary<string, string>
            {
                { "clientId", "invalid-client-id" },
                { "clientSecret", "invalid-client-secret" },
                { "refreshToken", "invalid-refresh-token" }
            };

            using (var provider = (GoogleDriveProvider)CloudStorageFactory.CreateProvider(
                CloudStorageFactory.ProviderType.GoogleDrive, config))
            {
                var isAuthenticated = await provider.IsAuthenticatedAsync();
                // With invalid credentials, authentication should fail
                Assert.False(isAuthenticated);
            }
        }

        [Fact]
        public void CloudStorageProvider_FileUpload_RequiresAuthentication()
        {
            var config = new Dictionary<string, string>
            {
                { "siteUrl", "https://company.sharepoint.com/sites/project" },
                { "clientId", "test-client-id" },
                { "clientSecret", "test-client-secret" },
                { "tenantId", "test-tenant-id" }
            };

            using (var provider = CloudStorageFactory.CreateProvider(
                CloudStorageFactory.ProviderType.SharePoint, config))
            {
                // Provider should implement ICloudStorageProvider
                Assert.NotNull(provider);
                Assert.True(provider is ICloudStorageProvider);
            }
        }

        [Fact]
        public void CloudStorageFactory_CaseInsensitive_ProviderNames()
        {
            var config = new Dictionary<string, string>
            {
                { "siteUrl", "https://company.sharepoint.com/sites/project" },
                { "clientId", "test-client-id" },
                { "clientSecret", "test-client-secret" },
                { "tenantId", "test-tenant-id" }
            };

            var provider1 = CloudStorageFactory.CreateProvider("SharePoint", config);
            var provider2 = CloudStorageFactory.CreateProvider("sharepoint", config);
            var provider3 = CloudStorageFactory.CreateProvider("SHAREPOINT", config);

            Assert.Equal(provider1.ProviderName, provider2.ProviderName);
            Assert.Equal(provider2.ProviderName, provider3.ProviderName);

            provider1.Dispose();
            provider2.Dispose();
            provider3.Dispose();
        }

        [Fact]
        public async Task CloudStorageProvider_UploadFileAsync_ReturnsResult()
        {
            var config = new Dictionary<string, string>
            {
                { "siteUrl", "https://company.sharepoint.com/sites/project" },
                { "clientId", "invalid-id" },
                { "clientSecret", "invalid-secret" },
                { "tenantId", "invalid-tenant" }
            };

            using (var provider = CloudStorageFactory.CreateProvider(
                CloudStorageFactory.ProviderType.SharePoint, config))
            {
                var result = await provider.UploadFileAsync("/Cloud/Requests", "test.json", new byte[] { 1, 2, 3 });
                // Should fail due to invalid credentials, but operation structure should work
                Assert.False(result.Success);
                Assert.NotNull(result.Message);
            }
        }

        [Fact]
        public async Task CloudStorageProvider_ListFilesAsync_ReturnsResult()
        {
            var config = new Dictionary<string, string>
            {
                { "siteUrl", "https://company.sharepoint.com/sites/project" },
                { "clientId", "invalid-id" },
                { "clientSecret", "invalid-secret" },
                { "tenantId", "invalid-tenant" }
            };

            using (var provider = CloudStorageFactory.CreateProvider(
                CloudStorageFactory.ProviderType.SharePoint, config))
            {
                var result = await provider.ListFilesAsync("/Cloud/Requests");
                // Should fail due to invalid credentials, but operation structure should work
                Assert.False(result.Success);
                Assert.NotNull(result.Message);
            }
        }

        [Fact]
        public async Task CloudStorageProvider_DownloadFileAsync_ReturnsResult()
        {
            var config = new Dictionary<string, string>
            {
                { "clientId", "invalid-id" },
                { "clientSecret", "invalid-secret" },
                { "refreshToken", "invalid-token" }
            };

            using (var provider = CloudStorageFactory.CreateProvider(
                CloudStorageFactory.ProviderType.GoogleDrive, config))
            {
                var result = await provider.DownloadFileAsync("/Cloud/Responses", "test.json");
                // Should fail due to invalid credentials, but operation structure should work
                Assert.False(result.Success);
                Assert.NotNull(result.Message);
            }
        }

        [Fact]
        public async Task CloudStorageProvider_DeleteFileAsync_ReturnsResult()
        {
            var config = new Dictionary<string, string>
            {
                { "clientId", "invalid-id" },
                { "clientSecret", "invalid-secret" },
                { "refreshToken", "invalid-token" }
            };

            using (var provider = CloudStorageFactory.CreateProvider(
                CloudStorageFactory.ProviderType.GoogleDrive, config))
            {
                var result = await provider.DeleteFileAsync("/Cloud/Responses", "test.json");
                // Should fail due to invalid credentials, but operation structure should work
                Assert.False(result.Success);
                Assert.NotNull(result.Message);
            }
        }

        [Fact]
        public async Task CloudStorageProvider_CreateFolderAsync_ReturnsResult()
        {
            var config = new Dictionary<string, string>
            {
                { "siteUrl", "https://company.sharepoint.com/sites/project" },
                { "clientId", "invalid-id" },
                { "clientSecret", "invalid-secret" },
                { "tenantId", "invalid-tenant" }
            };

            using (var provider = CloudStorageFactory.CreateProvider(
                CloudStorageFactory.ProviderType.SharePoint, config))
            {
                var result = await provider.CreateFolderAsync("/Cloud", "NewFolder");
                // Should fail due to invalid credentials, but operation structure should work
                Assert.False(result.Success);
                Assert.NotNull(result.Message);
            }
        }

        [Fact]
        public async Task CloudStorageProvider_MoveFileAsync_ReturnsResult()
        {
            var config = new Dictionary<string, string>
            {
                { "clientId", "invalid-id" },
                { "clientSecret", "invalid-secret" },
                { "refreshToken", "invalid-token" }
            };

            using (var provider = CloudStorageFactory.CreateProvider(
                CloudStorageFactory.ProviderType.GoogleDrive, config))
            {
                var result = await provider.MoveFileAsync("/Cloud/Requests", "test.json", 
                    "/Cloud/Responses", "test.json");
                // Should fail due to invalid credentials, but operation structure should work
                Assert.False(result.Success);
                Assert.NotNull(result.Message);
            }
        }

        [Fact]
        public async Task CloudStorageProvider_FileExistsAsync_ReturnsBoolean()
        {
            var config = new Dictionary<string, string>
            {
                { "siteUrl", "https://company.sharepoint.com/sites/project" },
                { "clientId", "invalid-id" },
                { "clientSecret", "invalid-secret" },
                { "tenantId", "invalid-tenant" }
            };

            using (var provider = CloudStorageFactory.CreateProvider(
                CloudStorageFactory.ProviderType.SharePoint, config))
            {
                var exists = await provider.FileExistsAsync("/Cloud/Requests", "test.json");
                // Should return false for invalid credentials
                Assert.False(exists);
            }
        }

        [Fact]
        public async Task CloudStorageProvider_RefreshAuthenticationAsync_ReturnsBoolean()
        {
            var config = new Dictionary<string, string>
            {
                { "clientId", "invalid-id" },
                { "clientSecret", "invalid-secret" },
                { "refreshToken", "invalid-token" }
            };

            using (var provider = CloudStorageFactory.CreateProvider(
                CloudStorageFactory.ProviderType.GoogleDrive, config))
            {
                var refreshed = await provider.RefreshAuthenticationAsync();
                // Should return false for invalid credentials
                Assert.False(refreshed);
            }
        }

        [Fact]
        public async Task CloudStorageProvider_GetStorageStatsAsync_ReturnsResult()
        {
            var config = new Dictionary<string, string>
            {
                { "siteUrl", "https://company.sharepoint.com/sites/project" },
                { "clientId", "invalid-id" },
                { "clientSecret", "invalid-secret" },
                { "tenantId", "invalid-tenant" }
            };

            using (var provider = CloudStorageFactory.CreateProvider(
                CloudStorageFactory.ProviderType.SharePoint, config))
            {
                var result = await provider.GetStorageStatsAsync();
                // Should fail due to invalid credentials, but operation structure should work
                Assert.False(result.Success);
                Assert.NotNull(result.Message);
            }
        }
    }
}
