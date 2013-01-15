using System;

namespace Platform
{

    /// <summary>
    /// Value object defining configuration of an Azure storage setup
    /// </summary>
    public sealed class AzureStoreConfiguration
    {
        public readonly string ConnectionString;
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