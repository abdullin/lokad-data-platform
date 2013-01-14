using System;
using System.Collections.Generic;
using System.IO;
using Platform.Storage;
using System.Linq;

namespace Platform.StreamClients
{
    public class FileStreamClient : JsonStreamClientBase, IRawStreamClient
    {

        readonly string _serverFolder;

        public FileStreamClient(string serverFolder, string serverEndpoint = null) : this(serverFolder, ContainerName.Create(ContainerName.Default),serverEndpoint)
        {
            
        }

        public FileStreamClient(string serverFolder, ContainerName container, string serverEndpoint = null) : base(container, serverEndpoint)
        {

            // open for reading or new
            _serverFolder = serverFolder;
            
            var path = Path.Combine(Path.GetFullPath(serverFolder ?? ""), container.Name);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }



        public IEnumerable<RetrievedDataRecord> ReadAll(StorageOffset startOffset, int maxRecordCount)
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



        public void WriteEventsInLargeBatch(string streamKey, IEnumerable<RecordForStaging> records)
        {
            if (!Directory.Exists(_serverFolder))
                Directory.CreateDirectory(_serverFolder);

            var location = Path.Combine(_serverFolder, Guid.NewGuid().ToString());
            try
            {
                var result = PrepareStaging(records, location);
                ImportEventsInternal(streamKey, location, result);
            }
            finally
            {
                //File.Delete(location);
            }
        }

        static long PrepareStaging(IEnumerable<RecordForStaging> records, string location)
        {
            try
            {
                using (var fs = FileMessageSet.CreateNew(location))
                {
                    return fs.Append("", records.Select(r =>
                        {
                            if (r.Data.Length > MessageSizeLimit)
                                throw new ArgumentException(string.Format("Messages can't be larger than {0} bytes",
                                    MessageSizeLimit));
                            
                            return r.Data;
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