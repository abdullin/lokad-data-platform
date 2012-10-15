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
    }




    public class FilePlatformClient
    {
        readonly string _serverFolder;
        readonly string _serverEndpoint;
        static readonly ILogger Log = LogManager.GetLoggerFor<FilePlatformClient>();

        public FilePlatformClient(string serverFolder, string serverEndpoint)
        {
            _serverFolder = serverFolder;
            _serverEndpoint = serverEndpoint;
            _client = new JsonServiceClient(_serverEndpoint);
        }

        readonly JsonServiceClient _client;
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
