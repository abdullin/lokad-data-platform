using System;
using System.Collections.Generic;
using System.IO;
using Platform.Storage;
using System.Linq;

namespace Platform.StreamClients
{
    public class FileStreamClient : JsonStreamClientBase, IInternalStreamClient
    {

        readonly string _serverFolder;
        

        readonly FileCheckpoint _checkpoint;
        readonly FileMessageSet _messageSet;

        static readonly ILogger Log = LogManager.GetLoggerFor<FileStreamClient>();

        public FileStreamClient(string serverFolder, string serverEndpoint = null) : this(serverFolder, ContainerName.Create(ContainerName.Default),serverEndpoint)
        {
            
        }

        public FileStreamClient(string serverFolder, ContainerName container, string serverEndpoint = null) : base(container, serverEndpoint)
        {
            _serverFolder = serverFolder;
            
            var path = Path.Combine(Path.GetFullPath(serverFolder ?? ""), container.Name);

            var checkpointName = Path.Combine(path, "stream.chk");
            var fileStreamName = Path.Combine(path, "stream.dat");

            _checkpoint = new FileCheckpoint(checkpointName);
            _messageSet = FileMessageSet.OpenForReadingOrNew(fileStreamName);

        }



        public IEnumerable<RetrievedDataRecord> ReadAll(StorageOffset startOffset, int maxRecordCount)
        {
            Ensure.Nonnegative(maxRecordCount, "maxRecordCount");


            var maxOffset = _checkpoint.ReadFile();

            // nothing to read from here
            if (startOffset >= new StorageOffset(maxOffset))
                yield break;

            int recordCount = 0;
            foreach (var msg in _messageSet.ReadAll(startOffset.OffsetInBytes, maxRecordCount))
            {
                yield return new RetrievedDataRecord(msg.StreamKey, msg.Message, new StorageOffset(msg.Offset));
                if (++recordCount >= maxRecordCount)
                    yield break;
                // we don't want to go above the initial water mark
                if (msg.NextOffset>=maxOffset)
                    yield break;
                
            }
        }



        public void WriteEventsInLargeBatch(string streamKey, IEnumerable<RecordForStaging> records)
        {
            if (!Directory.Exists(_serverFolder))
                Directory.CreateDirectory(_serverFolder);

            var location = Path.Combine(_serverFolder, Guid.NewGuid().ToString());
            try
            {
                PrepareStaging(records, location);
                ImportEventsInternal(streamKey, location);
            }
            finally
            {
                //File.Delete(location);
            }
        }

        static void PrepareStaging(IEnumerable<RecordForStaging> records, string location)
        {
            try
            {
                using (var fs = FileMessageSet.CreateNew(location))
                {
                    fs.Append("staging", records.Select(r =>
                        {
                            if (r.Data.Length > MessageSizeLimit)
                                throw new ArgumentException(string.Format("Messages can't be larger than {0} bytes",
                                    MessageSizeLimit));
                            
                            return r.Data;
                        }));
                    fs.Close();
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