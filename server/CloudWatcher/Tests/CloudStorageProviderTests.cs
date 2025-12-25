using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CloudWatcher.CloudStorage;
using Xunit;

namespace CloudWatcher.Tests
{
    public class CloudStorageProviderTests
    {
        [Fact]
        public void CloudStorageFactory_CreateSharePointProvider_WithValidConfig()
        {
            var config = new Dictionary<string, string>
            {
                { "siteUrl", "https://company.sharepoint.com/sites/project" },
                { "clientId", "test-client-id" },
                { "clientSecret", "test-client-secret" },
                { "tenantId", "test-tenant-id" }
            };

            var provider = CloudStorageFactory.CreateProvider(CloudStorageFactory.ProviderType.SharePoint, config);

            Assert.NotNull(provider);
            Assert.Equal("SharePoint", provider.ProviderName);
        }

        [Fact]
        public void CloudStorageFactory_CreateGoogleDriveProvider_WithValidConfig()
        {
            var config = new Dictionary<string, string>
            {
                { "clientId", "test-client-id" },
                { "clientSecret", "test-client-secret" },
                { "refreshToken", "test-refresh-token" }
            };

            var provider = CloudStorageFactory.CreateProvider(CloudStorageFactory.ProviderType.GoogleDrive, config);

            Assert.NotNull(provider);
            Assert.Equal("GoogleDrive", provider.ProviderName);
        }

        [Fact]
        public void CloudStorageFactory_CreateProvider_WithStringName_SharePoint()
        {
            var config = new Dictionary<string, string>
            {
                { "siteUrl", "https://company.sharepoint.com/sites/project" },
                { "clientId", "test-client-id" },
                { "clientSecret", "test-client-secret" },
                { "tenantId", "test-tenant-id" }
            };

            var provider = CloudStorageFactory.CreateProvider("sharepoint", config);

            Assert.NotNull(provider);
            Assert.Equal("SharePoint", provider.ProviderName);
        }

        [Fact]
        public void CloudStorageFactory_CreateProvider_WithStringName_GoogleDrive()
        {
            var config = new Dictionary<string, string>
            {
                { "clientId", "test-client-id" },
                { "clientSecret", "test-client-secret" },
                { "refreshToken", "test-refresh-token" }
            };

            var provider = CloudStorageFactory.CreateProvider("googledrive", config);

            Assert.NotNull(provider);
            Assert.Equal("GoogleDrive", provider.ProviderName);
        }

        [Fact]
        public void CloudStorageFactory_CreateSharePointProvider_MissingConfig_ThrowsException()
        {
            var config = new Dictionary<string, string>
            {
                { "siteUrl", "https://company.sharepoint.com/sites/project" },
                { "clientId", "test-client-id" }
                // Missing clientSecret and tenantId
            };

            Assert.Throws<ArgumentException>(() =>
                CloudStorageFactory.CreateProvider(CloudStorageFactory.ProviderType.SharePoint, config)
            );
        }

        [Fact]
        public void CloudStorageFactory_CreateGoogleDriveProvider_MissingConfig_ThrowsException()
        {
            var config = new Dictionary<string, string>
            {
                { "clientId", "test-client-id" }
                // Missing clientSecret and refreshToken
            };

            Assert.Throws<ArgumentException>(() =>
                CloudStorageFactory.CreateProvider(CloudStorageFactory.ProviderType.GoogleDrive, config)
            );
        }

        [Fact]
        public void CloudStorageFactory_CreateProvider_UnknownProvider_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
                CloudStorageFactory.CreateProvider("unknown", new Dictionary<string, string>())
            );
        }

        [Fact]
        public void CloudOperationResult_CreateSuccess_WithData()
        {
            var data = new { id = "123", name = "test" };
            var result = CloudOperationResult.CreateSuccess(data, "Operation succeeded");

            Assert.True(result.Success);
            Assert.Equal("Operation succeeded", result.Message);
            Assert.Equal(data, result.Data);
            Assert.Null(result.Exception);
        }

        [Fact]
        public void CloudOperationResult_CreateSuccess_WithoutData()
        {
            var result = CloudOperationResult.CreateSuccess();

            Assert.True(result.Success);
            Assert.Equal("Operation completed successfully", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public void CloudOperationResult_CreateFailure_WithException()
        {
            var ex = new InvalidOperationException("Test error");
            var result = CloudOperationResult.CreateFailure("Operation failed", ex);

            Assert.False(result.Success);
            Assert.Equal("Operation failed", result.Message);
            Assert.Equal(ex, result.Exception);
        }

        [Fact]
        public void CloudOperationResult_CreateFailure_WithoutException()
        {
            var result = CloudOperationResult.CreateFailure("Operation failed");

            Assert.False(result.Success);
            Assert.Equal("Operation failed", result.Message);
            Assert.Null(result.Exception);
        }

        [Fact]
        public void CloudFile_Properties_Set()
        {
            var file = new CloudFile
            {
                Id = "file-123",
                Name = "document.pdf",
                Size = 1024000,
                ModifiedDate = DateTime.UtcNow,
                ContentType = "application/pdf",
                IsFolder = false
            };

            Assert.Equal("file-123", file.Id);
            Assert.Equal("document.pdf", file.Name);
            Assert.Equal(1024000, file.Size);
            Assert.False(file.IsFolder);
            Assert.Equal("application/pdf", file.ContentType);
        }

        [Fact]
        public void CloudFolder_Properties_Set()
        {
            var now = DateTime.UtcNow;
            var folder = new CloudFolder
            {
                Id = "folder-456",
                Name = "MyFolder",
                CreatedDate = now,
                ParentId = "parent-789"
            };

            Assert.Equal("folder-456", folder.Id);
            Assert.Equal("MyFolder", folder.Name);
            Assert.Equal(now, folder.CreatedDate);
            Assert.Equal("parent-789", folder.ParentId);
        }
    }
}
