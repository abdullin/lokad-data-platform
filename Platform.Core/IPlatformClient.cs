using System;
using System.Collections.Generic;
using System.IO;
using Platform.Messages;
using Platform.Storage;
using ServiceStack.ServiceClient.Web;

namespace Platform
{
    public interface IPlatformClient
    {
        bool IsAzure { get; }
        IEnumerable<RetrievedDataRecord> ReadAll(long startOffset, int maxRecordCount = int.MaxValue);
        void WriteEvent(string streamName, byte[] data);
        void ImportBatch(string streamName, IEnumerable<RecordForStaging> records);
    }


    public class FilePlatformClient : IPlatformClient
    {
        readonly string _serverFolder;
        readonly string _serverEndpoint;

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

                        if (dataStream.Position + length > dataStream.Length)
                            throw new InvalidOperationException("Data length is out of range.");

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
                using (var checkBits = new BitReader(checkStream))
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

        public FilePlatformClient(string serverFolder, string serverEndpoint = null)
        {
            _serverFolder = serverFolder;
            _serverEndpoint = serverEndpoint;

            if (!string.IsNullOrWhiteSpace(_serverEndpoint))
            {
                _client = new JsonServiceClient(_serverEndpoint);
            }

            var path = Path.GetFullPath(serverFolder ?? "");

            _checkStreamName = Path.Combine(path, "stream.chk");
            _fileStreamName = Path.Combine(path, "stream.dat");
        }

        readonly JsonServiceClient _client;

        public void WriteEvent(string streamName, byte[] data)
        {
            var response = _client.Post<ClientDto.WriteEventResponse>("/stream", new ClientDto.WriteEvent()
            {
                Data = data,
                Stream = streamName
            });
            if (!response.Success)
                throw new InvalidOperationException(response.Result ?? "Client error");
        }

        public void ImportBatch(string streamName, IEnumerable<RecordForStaging> records)
        {
            if (!Directory.Exists(_serverFolder))
                Directory.CreateDirectory(_serverFolder);

            var name = Path.Combine(_serverFolder, Guid.NewGuid().ToString());
            try
            {
                using (var fs = File.OpenWrite(name))
                using (var bin = new BinaryWriter(fs))
                {
                    foreach (var record in records)
                    {
                        bin.Write(record.Data.Length);
                        bin.Write(record.Data);
                    }
                }

                var response = _client.Post<ClientDto.ImportEventsResponse>("/import", new ClientDto.ImportEvents()
                {
                    Location = name,
                    Stream = streamName,
                });

                if (!response.Success)
                    throw new InvalidOperationException(response.Result ?? "Client error");

            }
            finally
            {
                File.Delete(name);
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
