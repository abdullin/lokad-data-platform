using System;

namespace Platform
{

    /// <summary>
    /// Value object defining configuration of an Azure storage setup of Data platform
    /// server and client. It includes <see cref="ConnectionString"/> for Azure storage
    /// account and also <see cref="RootBlobContainerName"/> which will be used for this
    /// deployment (all data will reside inside)
    /// </summary>
    public sealed class AzureStoreConfiguration
    {
        /// <summary>
        /// How to connect to Azure storage account
        /// </summary>
        public readonly string ConnectionString;
        /// <summary>
        /// Name of the container in which all data for a given setup will reside
        /// </summary>
        public readonly string RootBlobContainerName;

        public AzureStoreConfiguration(string connectionString, string rootBlobContainerName)
        {
            RootBlobContainerName = rootBlobContainerName;
            ConnectionString = connectionString;
        }

        public static bool TryParse(string source, out AzureStoreConfiguration configuration)
        {
            const StringComparison icic = StringComparison.InvariantCultureIgnoreCase;
            if (source.StartsWith("DefaultEndpointsProtocol=", icic)
                || source.StartsWith("UseDevelopmentStorage=true", icic))
            {
                var parts = source.Split('|');

                string rootBlobContainerName = "dp-store";
                if (parts.Length>1)
                {
                    rootBlobContainerName = parts[1];
                }
                configuration = new AzureStoreConfiguration(parts[0], rootBlobContainerName);
                return true;
            }
            configuration = null;
            return false;

        }
    }
}