using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.StorageClient;
using Platform.StreamClients;

namespace Platform.StreamStorage.Azure
{
    public sealed class AzureEventStore : IDisposable
    {
        public readonly EventStoreId Container;
        readonly AzureEventStoreChunk _store;
        readonly AzureEventPointer _checkpoint;

        public AzureEventStore(EventStoreId container, AzureEventStoreChunk store, AzureEventPointer checkpoint)
        {
            Container = container;
            _store = store;
            _checkpoint = checkpoint;
        }

        public static bool TryGetContainerName
            (
            AzureStoreConfiguration config, 
            CloudBlobDirectory dir,
            out EventStoreId container)
        {
            var topic = dir.Uri.ToString().Remove(0, dir.Container.Uri.ToString().Length).Trim('/');

            container = null;
            if (EventStoreId.IsValid(topic)!= EventStoreId.Rule.Valid)
                return false;
            container = EventStoreId.Create(topic);
            return IsValid(config, container);
        }

        public static bool IsValid(AzureStoreConfiguration config, EventStoreId container)
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


        public static AzureEventStore OpenExistingForWriting(AzureStoreConfiguration config, EventStoreId container)
        {
            var blob = config.GetPageBlob(container.Name + "/stream.dat");
            var check = AzureEventPointer.OpenWriteable(blob);
            var offset = check.Read();
            var length = blob.Properties.Length;
            var store = AzureEventStoreChunk.OpenExistingForWriting(blob, offset, length);
            return new AzureEventStore(container, store, check);
        }
        public static AzureEventStore CreateNewForWriting(AzureStoreConfiguration config, EventStoreId container)
        {
            var blob = config.GetPageBlob(container.Name + "/stream.dat");
            blob.Container.CreateIfNotExist();

            var store = AzureEventStoreChunk.CreateNewForWriting(blob);
            var check = AzureEventPointer.OpenWriteable(blob);

            return new AzureEventStore(container, store, check);
        }

        public static AzureEventStore OpenExistingForReading(AzureStoreConfiguration config, EventStoreId container)
        {
            var blob = config.GetPageBlob(container.Name + "/stream.dat");
            var check = AzureEventPointer.OpenReadable(blob);
            blob.FetchAttributes();
            var store = AzureEventStoreChunk.OpenExistingForReading(blob, blob.Properties.Length);
            return new AzureEventStore(container, store, check);
        }

        public void Reset()
        {
            _store.Reset();
            _checkpoint.Write(0);
        }

        public IEnumerable<RetrievedEventsWithMetaData> ReadAll(EventStoreOffset startOffset, int maxRecordCount)
        {
            Ensure.Nonnegative(maxRecordCount, "maxRecordCount");


            var maxOffset = _checkpoint.Read();

            // nothing to read from here
            if (startOffset >= new EventStoreOffset(maxOffset))
                yield break;

            int recordCount = 0;
            foreach (var msg in _store.ReadAll(startOffset.OffsetInBytes, maxOffset, maxRecordCount))
            {
                yield return msg;
                if (++recordCount >= maxRecordCount)
                    yield break;
                // we don't want to go above the initial water mark
                if (msg.Next.OffsetInBytes >= maxOffset)
                    yield break;

            }
        }

        public void Write(string streamId, IEnumerable<byte[]> eventData)
        {
            var result = _store.Append(streamId, eventData);
            _checkpoint.Write(result.ChunkPosition);
        }

        public void Dispose()
        {
            
        }
    }
}