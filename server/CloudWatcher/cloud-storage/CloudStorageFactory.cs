using System;
using System.Collections.Generic;

namespace CloudWatcher.CloudStorage
{
    /// <summary>
    /// Factory for creating cloud storage provider instances
    /// Supports both SharePoint and Google Drive providers
    /// </summary>
    public class CloudStorageFactory
    {
        /// <summary>
        /// Provider type enumeration
        /// </summary>
        public enum ProviderType
        {
            SharePoint,
            GoogleDrive
        }

        /// <summary>
        /// Create a cloud storage provider instance
        /// </summary>
        /// <param name="providerType">Type of provider to create</param>
        /// <param name="config">Configuration dictionary with required parameters</param>
        /// <returns>Configured provider instance</returns>
        public static ICloudStorageProvider CreateProvider(ProviderType providerType, Dictionary<string, string> config)
        {
            return providerType switch
            {
                ProviderType.SharePoint => CreateSharePointProvider(config),
                ProviderType.GoogleDrive => CreateGoogleDriveProvider(config),
                _ => throw new ArgumentException($"Unknown provider type: {providerType}")
            };
        }

        /// <summary>
        /// Create provider by name string
        /// </summary>
        public static ICloudStorageProvider CreateProvider(string providerName, Dictionary<string, string> config)
        {
            return providerName.ToLower() switch
            {
                "sharepoint" => CreateSharePointProvider(config),
                "googledrive" => CreateGoogleDriveProvider(config),
                _ => throw new ArgumentException($"Unknown provider: {providerName}")
            };
        }

        private static ICloudStorageProvider CreateSharePointProvider(Dictionary<string, string> config)
        {
            var required = new[] { "siteUrl", "clientId", "clientSecret", "tenantId" };
            ValidateConfig(config, required);

            return new SharePointProvider(
                config["siteUrl"],
                config["clientId"],
                config["clientSecret"],
                config["tenantId"]
            );
        }

        private static ICloudStorageProvider CreateGoogleDriveProvider(Dictionary<string, string> config)
        {
            var required = new[] { "clientId", "clientSecret", "refreshToken" };
            ValidateConfig(config, required);

            return new GoogleDriveProvider(
                config["clientId"],
                config["clientSecret"],
                config["refreshToken"]
            );
        }

        private static void ValidateConfig(Dictionary<string, string> config, string[] requiredKeys)
        {
            foreach (var key in requiredKeys)
            {
                if (!config.ContainsKey(key) || string.IsNullOrWhiteSpace(config[key]))
                {
                    throw new ArgumentException($"Missing required configuration: {key}");
                }
            }
        }
    }
}
