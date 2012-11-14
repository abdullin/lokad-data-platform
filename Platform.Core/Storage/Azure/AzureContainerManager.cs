using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Platform.Storage.Azure
{
    public class AzureContainerManager : IDisposable
    {
        readonly AzureStoreConfiguration _config;
        readonly ILogger Log = LogManager.GetLoggerFor<AzureContainerManager>();
        readonly IDictionary<string, AzureContainer> _stores = new Dictionary<string, AzureContainer>();

        public AzureContainerManager(AzureStoreConfiguration config)
        {
            _config = config;

            var account = CloudStorageAccount.Parse(config.ConnectionString);
            var client = account.CreateCloudBlobClient();

            var container = client.GetContainerReference(config.Container);
            var blobs = container.ListBlobs();

            foreach (var blob in blobs)
            {
                var dir = blob as CloudBlobDirectory;

                if (dir == null) continue;

                var topic = dir.Uri.ToString().Remove(0, dir.Container.Uri.ToString().Length).Trim('/'); 
                var containerName = ContainerName.Create(topic);

                if (AzureContainer.ExistsValid(_config, containerName))
                {
                    var value = new AzureContainer(containerName, new AzureMessageSet(config, containerName));
                    _stores.Add(topic, value);
                }
                else
                {
                    Log.Error("Skipping invalid folder {0}", topic);
                }
            }
        }

        public void Reset()
        {
            foreach (var store in _stores.Values)
            {
                store.Reset();
            }
        }

        public void Append(ContainerName container, string streamKey, IEnumerable<byte[]> data)
        {
            AzureContainer store;
            if (!_stores.TryGetValue(container.Name, out store))
            {
                store = new AzureContainer(container, new AzureMessageSet(_config, container));
                _stores.Add(container.Name, store);
            }
            store.Write(streamKey, data);
        }

        public void Dispose()
        {

        }
    }

    public sealed class AzureContainer
    {
        public readonly ContainerName Container;
        public readonly AzureMessageSet Store;

        public AzureContainer(ContainerName container, AzureMessageSet store)
        {
            Container = container;
            Store = store;
        }

        public static bool ExistsValid(AzureStoreConfiguration config, ContainerName container)
        {
            // this is metadata checkpoint
            // var check = config.GetPageBlob(container.Name + "/stream.chk");
            var store = config.GetPageBlob(container.Name + "/stream.dat");
            return store.Exists();
        }

        public void Reset()
        {
            Store.Reset();
        }

        public void Write(string streamKey, IEnumerable<byte[]> data)
        {
            Store.Append(streamKey, data);
            //Checkpoint.Write(position);
        }

    }
}