using System;
using System.Collections.Generic;
using System.IO;
using Platform.Storage;

namespace Platform.StreamClients
{
    public class FileStreamClient : JsonStreamClientBase, IInternalStreamClient
    {

        readonly string _serverFolder;
        readonly string _checkStreamName;
        readonly string _fileStreamName;

        static readonly ILogger Log = LogManager.GetLoggerFor<FileStreamClient>();

        public FileStreamClient(string serverFolder, string serverEndpoint = null) : this(serverFolder, ContainerName.Create(ContainerName.Default),serverEndpoint)
        {
            
        }

        public FileStreamClient(string serverFolder, ContainerName container, string serverEndpoint = null) : base(container, serverEndpoint)
        {
            _serverFolder = serverFolder;
            
            var path = Path.Combine(Path.GetFullPath(serverFolder ?? ""), container.Name);

            _checkStreamName = Path.Combine(path,"stream.chk");
            _fileStreamName = Path.Combine(path,"stream.dat");
        }



        public IEnumerable<RetrievedDataRecord> ReadAll(StorageOffset startOffset, int maxRecordCount)
        {
            if (maxRecordCount < 0)
                throw new ArgumentOutOfRangeException("maxRecordCount");

            var maxOffset = GetMaxOffset();

            // nothing to read from here
            if (startOffset >= maxOffset)
                yield break;


            if (!File.Exists(_fileStreamName))
                throw new InvalidOperationException("File stream.chk found but stream.dat file does not exist");

            using (var dataStream = new FileStream(_fileStreamName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var dataBits = new BitReader(dataStream))
            {
                var seekResult = dataStream.Seek(startOffset.OffsetInBytes, SeekOrigin.Begin);

                if (startOffset.OffsetInBytes != seekResult)
                    throw new InvalidOperationException("Failed to reach position we seeked for");

                int recordCount = 0;
                while (true)
                {
                    var key = dataBits.ReadString();
                    var length = dataBits.Reader7BitInt();
                    var data = dataBits.ReadBytes(length);

                    var currentOffset = new StorageOffset(dataStream.Position);
                    yield return new RetrievedDataRecord(key, data, currentOffset);

                    recordCount += 1;
                    if (recordCount >= maxRecordCount)
                        yield break;

                    if (currentOffset >= maxOffset)
                        yield break;
                }

            }
        }

        private StorageOffset GetMaxOffset()
        {

            if (!File.Exists(_checkStreamName))
                return StorageOffset.Zero;

            using (var checkStream = new FileStream(_checkStreamName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var checkBits = new BinaryReader(checkStream))
                {
                    return new StorageOffset(checkBits.ReadInt64());
                }
            }

        }

        sealed class BitReader : BinaryReader
        {
            public BitReader(Stream output) : base(output) { }

            public int Reader7BitInt()
            {
                return Read7BitEncodedInt();
            }
        }

        public void WriteEventsInLargeBatch(string streamName, IEnumerable<RecordForStaging> records)
        {
            if (!Directory.Exists(_serverFolder))
                Directory.CreateDirectory(_serverFolder);

            var location = Path.Combine(_serverFolder, Guid.NewGuid().ToString());
            try
            {
                PrepareStaging(records, location);
                ImportEventsInternal(streamName, location);
            }
            finally
            {
                //File.Delete(location);
            }
        }

        static void PrepareStaging(IEnumerable<RecordForStaging> records, string location)
        {
            using (var fs = File.OpenWrite(location))
            using (var bin = new BinaryWriter(fs))
            {
                foreach (var record in records)
                {
                    if (record.Data.Length > MessageSizeLimit)
                        throw new ArgumentException(string.Format("Messages can't be larger than {0} bytes", MessageSizeLimit));

                    bin.Write(record.Data.Length);
                    bin.Write(record.Data);
                }
            }
        }
    }
}