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

            var rootAzureContainer = client.GetContainerReference(config.Container);

            foreach (var blob in rootAzureContainer.ListBlobs())
            {
                var dir = blob as CloudBlobDirectory;

                if (dir == null) continue;

                ContainerName container;

                if (AzureContainer.TryGetContainerName(_config, dir, out container))
                {
                    var value = AzureContainer.OpenExistingForWriting(config, container);
                    _stores.Add(container.Name, value);
                }
                else
                {
                    Log.Error("Skipping invalid folder {0}", rootAzureContainer.Uri.MakeRelativeUri(dir.Uri));
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
                store = AzureContainer.CreateNewForWriting(_config, container);
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
        readonly AzureMessageSet _store;

        public AzureContainer(ContainerName container, AzureMessageSet store)
        {
            Container = container;
            _store = store;
        }

        public static bool TryGetContainerName
        (
            AzureStoreConfiguration config, 
            CloudBlobDirectory dir,
            out ContainerName container)
        {

            // this is metadata checkpoint
            // var check = config.GetPageBlob(container.Name + "/stream.chk");

            var topic = dir.Uri.ToString().Remove(0, dir.Container.Uri.ToString().Length).Trim('/');

            container = null;
            if (ContainerName.IsValid(topic)!= ContainerName.Rule.Valid)
                return false;
            container = ContainerName.Create(topic);
            var store = config.GetPageBlob(container.Name + "/stream.dat");
            return store.Exists();
        }

        public static AzureContainer OpenExistingForWriting(AzureStoreConfiguration config, ContainerName container)
        {
            return new AzureContainer(container, new AzureMessageSet(config, container));
        }
        public static AzureContainer CreateNewForWriting(AzureStoreConfiguration config, ContainerName container)
        {
            return new AzureContainer(container, new AzureMessageSet(config, container));
        }

        public void Reset()
        {
            _store.Reset();
        }

        public void Write(string streamKey, IEnumerable<byte[]> data)
        {
            _store.Append(streamKey, data);
            //Checkpoint.Write(position);
        }

    }
}