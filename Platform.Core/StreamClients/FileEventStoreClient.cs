using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Platform.StreamStorage;
using Platform.StreamStorage.File;

namespace Platform.StreamClients
{
    public class FileEventStoreClient : JsonEventStoreClientBase, IRawEventStoreClient
    {
        readonly string _serverFolder;

        public FileEventStoreClient(string serverFolder, EventStoreId storeId, string serverEndpoint = null) : base(storeId, serverEndpoint)
        {

            // open for reading or new
            _serverFolder = serverFolder;
            
            var path = Path.Combine(Path.GetFullPath(serverFolder ?? ""), storeId.Name);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }



        public IEnumerable<RetrievedEventsWithMetaData> ReadAllEvents(EventStoreOffset startOffset, int maxRecordCount)
        {
            if (!FileEventStore.ExistsValid(_serverFolder, StoreId))
                yield break;

            using (var store = FileEventStore.OpenForReading(_serverFolder, StoreId))
            {
                foreach (var record in store.ReadAll(startOffset, maxRecordCount))
                {
                    yield return record;
                }
            }
        }



        public void WriteEventsInLargeBatch(string streamId, IEnumerable<byte[]> eventData)
        {
            if (!Directory.Exists(_serverFolder))
                Directory.CreateDirectory(_serverFolder);

            var location = Path.Combine(_serverFolder, Guid.NewGuid().ToString());
            try
            {
                var length = PrepareStaging(eventData, location);
                ImportEventsInternal(streamId, location, length);
            }
            catch(PlatformClientException)
            {
                File.Delete(location);
                throw;
            }
        }

        static long PrepareStaging(IEnumerable<byte[]> eventData, string location)
        {
            using (var fs = FileEventStoreChunk.CreateNew(location))
            {
                var result = fs.Append("", eventData.Select(r =>
                {
                    if (r.Length > MessageSizeLimit)
                        throw new PlatformClientException(string.Format("Messages can't be larger than {0} bytes", MessageSizeLimit));

                    return r;
                }));

                if (result.WrittenEvents == 0)
                    throw new PlatformClientException("At least one event is expected in batch");
                return result.WrittenBytes;
            }
            
        }
    }
}