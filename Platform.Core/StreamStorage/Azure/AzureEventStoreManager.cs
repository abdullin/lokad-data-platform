using System.Collections.Generic;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Platform.StreamStorage.Azure
{
    /// <summary>
    /// Event store based Windows Azure Blob Storage.
    /// See documentation of the <c>IEventStoreManager</c>.
    /// </summary>
    public class AzureEventStoreManager : IEventStoreManager
    {
        readonly AzureStoreConfiguration _config;
        readonly ILogger Log = LogManager.GetLoggerFor<AzureEventStoreManager>();
        readonly IDictionary<string, AzureEventStore> _stores = new Dictionary<string, AzureEventStore>();

        public AzureEventStoreManager(AzureStoreConfiguration config)
        {
            _config = config;

            var account = CloudStorageAccount.Parse(config.ConnectionString);
            var client = account.CreateCloudBlobClient();

            var rootAzureContainer = client.GetContainerReference(config.RootBlobContainerName);

            foreach (var blob in rootAzureContainer.ListBlobs())
            {
                var dir = blob as CloudBlobDirectory;

                if (dir == null) continue;

                EventStoreId container;

                if (AzureEventStore.TryGetContainerName(_config, dir, out container))
                {
                    var value = AzureEventStore.OpenExistingForWriting(config, container);
                    _stores.Add(container.Name, value);
                }
                else
                {
                    Log.Info("Skipping invalid folder {0}", rootAzureContainer.Uri.MakeRelativeUri(dir.Uri));
                }
            }
        }

        public void ResetAllStores()
        {
            foreach (var store in _stores.Values)
            {
                store.Reset();
            }
        }

        public void AppendEventsToStore(EventStoreId storeId, string streamId, IEnumerable<byte[]> eventData)
        {
            AzureEventStore store;
            if (!_stores.TryGetValue(storeId.Name, out store))
            {
                store = AzureEventStore.CreateNewForWriting(_config, storeId);
                _stores.Add(storeId.Name, store);
            }
            store.Write(streamId, eventData);
        }

        public void Dispose()
        {

        }

    }
}