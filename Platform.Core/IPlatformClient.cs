using System;
using System.Collections.Generic;
using System.IO;
using Platform.Storage;

namespace Platform
{
    public interface IPlatformClient
    {
        bool IsAzure { get; }
        IEnumerable<RetrievedDataRecord> ReadAll(long startOffset, int maxRecordCount = int.MaxValue);
        void WriteEvent(string streamName, byte[] data);
        void ImportBatch(string streamName, IEnumerable<RecordForStaging> records);
    }

    public class FilePlatformClient : JsonPlatformClientBase, IPlatformClient
    {
        readonly string _serverFolder;
        

        readonly string _checkStreamName;
        readonly string _fileStreamName;

        static readonly ILogger Log = LogManager.GetLoggerFor<FilePlatformClient>();

        
        public IEnumerable<RetrievedDataRecord> ReadAll(long startOffset, int maxRecordCount)
        {
            if (startOffset < 0)
                throw new ArgumentOutOfRangeException("startOffset");

            if (maxRecordCount < 0)
                throw new ArgumentOutOfRangeException("maxRecordCount");

            var endOffset = GetEndOffset();

            if (startOffset >= endOffset)
                yield break;


            if (!File.Exists(_fileStreamName))
                throw new InvalidOperationException("File stream.chk found but stream.dat file does not exist");

            using (var dataStream = new FileStream(_fileStreamName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var dataBits = new BitReader(dataStream))
                {
                    dataStream.Seek(startOffset, SeekOrigin.Begin);


                    int count = 0;
                    while (dataStream.Position < endOffset && count <= maxRecordCount)
                    {
                        var key = dataBits.ReadString();
                        var length = dataBits.Reader7BitInt();

                        var data = dataBits.ReadBytes(length);
                        yield return new RetrievedDataRecord(key, data, dataStream.Position);

                        if (count == maxRecordCount)
                            break;

                        count++;
                    }
                }
            }
        }
        public bool IsAzure { get { return false; } }


        private long GetEndOffset()
        {

            if (!File.Exists(_checkStreamName))
                return 0;

            using (var checkStream = new FileStream(_checkStreamName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var checkBits = new BinaryReader(checkStream))
                {
                    return checkBits.ReadInt64();
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

        public FilePlatformClient(string serverFolder, string serverEndpoint = null) : base(serverEndpoint)
        {
            _serverFolder = serverFolder;
            
            var path = Path.GetFullPath(serverFolder ?? "");

            _checkStreamName = Path.Combine(path, "stream.chk");
            _fileStreamName = Path.Combine(path, "stream.dat");
        }

        public void ImportBatch(string streamName, IEnumerable<RecordForStaging> records)
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
                File.Delete(location);
            }
        }

        static void PrepareStaging(IEnumerable<RecordForStaging> records, string location)
        {
            using (var fs = File.OpenWrite(location))
            using (var bin = new BinaryWriter(fs))
            {
                foreach (var record in records)
                {
                    bin.Write(record.Data.Length);
                    bin.Write(record.Data);
                }
            }
        }
    }

    public struct RecordForStaging
    {
        public readonly byte[] Data;
        public RecordForStaging(byte[] data)
        {
            Data = data;
        }
    }
}
