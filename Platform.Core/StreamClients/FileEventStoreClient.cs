using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Platform.StreamStorage;
using Platform.StreamStorage.File;

namespace Platform.StreamClients
{
    public class FileEventStoreClient : JsonStreamClientBase, IRawEventStoreClient
    {

        readonly string _serverFolder;

        public FileEventStoreClient(string serverFolder, string serverEndpoint = null) : this(serverFolder, EventStoreId.Create(EventStoreId.Default),serverEndpoint)
        {
            
        }

        public FileEventStoreClient(string serverFolder, EventStoreId storeId, string serverEndpoint = null) : base(storeId, serverEndpoint)
        {

            // open for reading or new
            _serverFolder = serverFolder;
            
            var path = Path.Combine(Path.GetFullPath(serverFolder ?? ""), storeId.Name);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }



        public IEnumerable<RetrievedEventWithMetaData> ReadAllEvents(StorageOffset startOffset, int maxRecordCount)
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
                var result = PrepareStaging(eventData, location);
                ImportEventsInternal(streamId, location, result);
            }
            finally
            {
                //File.Delete(location);
            }
        }

        static long PrepareStaging(IEnumerable<byte[]> eventData, string location)
        {
            try
            {
                using (var fs = FileMessageSet.CreateNew(location))
                {
                    return fs.Append("", eventData.Select(r =>
                        {
                            if (r.Length > MessageSizeLimit)
                                throw new ArgumentException(string.Format("Messages can't be larger than {0} bytes",
                                    MessageSizeLimit));
                            
                            return r;
                        }));
                }
            }
            catch(Exception)
            {
                File.Delete(location);
                throw;
            }
        }
    }
}