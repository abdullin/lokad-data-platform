using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Platform.StreamStorage;
using Platform.StreamStorage.File;

namespace Platform.StreamClients
{
    public class FileStreamClient : JsonStreamClientBase, IRawStreamClient
    {

        readonly string _serverFolder;

        public FileStreamClient(string serverFolder, string serverEndpoint = null) : this(serverFolder, EventStoreName.Create(EventStoreName.Default),serverEndpoint)
        {
            
        }

        public FileStreamClient(string serverFolder, EventStoreName container, string serverEndpoint = null) : base(container, serverEndpoint)
        {

            // open for reading or new
            _serverFolder = serverFolder;
            
            var path = Path.Combine(Path.GetFullPath(serverFolder ?? ""), container.Name);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }



        public IEnumerable<RetrievedEventWithMetaData> ReadAllEvents(StorageOffset startOffset, int maxRecordCount)
        {
            if (!FileContainer.ExistsValid(_serverFolder, Container))
                yield break;

            using (var container = FileContainer.OpenForReading(_serverFolder, Container))
            {
                foreach (var record in container.ReadAll(startOffset, maxRecordCount))
                {
                    yield return record;
                }
            }
        }



        public void WriteEventsInLargeBatch(string streamName, IEnumerable<byte[]> eventData)
        {
            if (!Directory.Exists(_serverFolder))
                Directory.CreateDirectory(_serverFolder);

            var location = Path.Combine(_serverFolder, Guid.NewGuid().ToString());
            try
            {
                var result = PrepareStaging(eventData, location);
                ImportEventsInternal(streamName, location, result);
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