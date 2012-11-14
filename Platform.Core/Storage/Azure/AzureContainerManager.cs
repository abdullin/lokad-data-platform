using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Platform.StreamClients;

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

    public sealed class AzureContainer : IDisposable
    {
        public readonly ContainerName Container;
        readonly AzureMessageSet _store;
        readonly AzureMetadataCheckpoint _checkpoint;

        public AzureContainer(ContainerName container, AzureMessageSet store, AzureMetadataCheckpoint checkpoint)
        {
            Container = container;
            _store = store;
            _checkpoint = checkpoint;
        }

        public static bool TryGetContainerName
        (
            AzureStoreConfiguration config, 
            CloudBlobDirectory dir,
            out ContainerName container)
        {
            var topic = dir.Uri.ToString().Remove(0, dir.Container.Uri.ToString().Length).Trim('/');

            container = null;
            if (ContainerName.IsValid(topic)!= ContainerName.Rule.Valid)
                return false;
            container = ContainerName.Create(topic);
            return IsValid(config, container);
        }

        public static bool IsValid(AzureStoreConfiguration config, ContainerName container)
        {
            var store = config.GetPageBlob(container.Name + "/stream.dat");
            return Exists(store);
        }

        static bool Exists(CloudBlob blob)
        {
            try
            {
                blob.FetchAttributes();
                return true;
            }
            catch (StorageClientException e)
            {
                switch (e.ErrorCode)
                {
                        case StorageErrorCode.ContainerNotFound:
                        case StorageErrorCode.ResourceNotFound:
                        return false;
                }
                throw;
            }
        }


        public static AzureContainer OpenExistingForWriting(AzureStoreConfiguration config, ContainerName container)
        {
            var blob = config.GetPageBlob(container.Name + "/stream.dat");
            var check = AzureMetadataCheckpoint.OpenWriteable(blob);
            var offset = check.Read();
            var length = blob.Properties.Length;
            var store = AzureMessageSet.OpenExistingForWriting(blob, offset, length);
            return new AzureContainer(container, store, check);
        }
        public static AzureContainer CreateNewForWriting(AzureStoreConfiguration config, ContainerName container)
        {
            var blob = config.GetPageBlob(container.Name + "/stream.dat");
            blob.Container.CreateIfNotExist();

            var store = AzureMessageSet.CreateNewForWriting(blob);
            var check = AzureMetadataCheckpoint.OpenWriteable(blob);

            return new AzureContainer(container, store, check);
        }

        public static AzureContainer OpenExistingForReading(AzureStoreConfiguration config, ContainerName container)
        {
            var blob = config.GetPageBlob(container.Name + "/stream.dat");
            var check = AzureMetadataCheckpoint.OpenReadable(blob);
            var store = AzureMessageSet.OpenExistingForReading(blob, blob.Properties.Length);
            return new AzureContainer(container, store, check);
        }

        public void Reset()
        {
            _store.Reset();
            _checkpoint.Write(0);
        }

        public IEnumerable<RetrievedDataRecord> ReadAll(StorageOffset startOffset, int maxRecordCount)
        {
            Ensure.Nonnegative(maxRecordCount, "maxRecordCount");


            var maxOffset = _checkpoint.Read();

            // nothing to read from here
            if (startOffset >= new StorageOffset(maxOffset))
                yield break;

            int recordCount = 0;
            foreach (var msg in _store.ReadAll(startOffset.OffsetInBytes, maxOffset, maxRecordCount))
            {
                yield return new RetrievedDataRecord(msg.Key, msg.Data, msg.Next);
                if (++recordCount >= maxRecordCount)
                    yield break;
                // we don't want to go above the initial water mark
                if (msg.Next.OffsetInBytes >= maxOffset)
                    yield break;

            }
        }

        public void Write(string streamKey, IEnumerable<byte[]> data)
        {
            var result = _store.Append(streamKey, data);
            _checkpoint.Write(result);
        }

        public void Dispose()
        {
            
        }
    }
}