using System;

namespace Platform.Storage.Azure
{


    public sealed class AzureStoreConfiguration
    {
        public readonly string ConnectionString;
        public readonly string Container;

        public AzureStoreConfiguration(string connectionString, string container)
        {
            Container = container;
            ConnectionString = connectionString;
        }

        public static bool TryParse(string source, out AzureStoreConfiguration configuration)
        {
            const StringComparison icic = StringComparison.InvariantCultureIgnoreCase;
            if (source.StartsWith("DefaultEndpointsProtocol=", icic)
                || source.StartsWith("UseDevelopmentStorage=true", icic))
            {
                var parts = source.Split('|');

                string container = "dp-store";
                if (parts.Length>1)
                {
                    container = parts[1];
                }
                configuration = new AzureStoreConfiguration(parts[0], container);
                return true;
            }
            configuration = null;
            return false;

        }
    }
}