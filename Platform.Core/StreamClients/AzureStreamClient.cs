using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.WindowsAzure.StorageClient;
using Platform.Storage;
using Platform.Storage.Azure;

namespace Platform.StreamClients
{
    public class AzureStreamClient : JsonStreamClientBase, IInternalStreamClient
    {
        public AzureStoreConfiguration Config { get; set; }
        readonly CloudPageBlob _blob;

        static readonly ILogger Log = LogManager.GetLoggerFor<AzureStreamClient>();

        public AzureStreamClient(AzureStoreConfiguration config, ContainerName container, string serverEndpoint = null)
            : base(container, serverEndpoint)
        {
            Config = config;
            _blob = config.GetPageBlob(container.Name + "/stream.dat");
            _blob.Container.CreateIfNotExist();
        }

        public IEnumerable<RetrievedDataRecord> ReadAll(StorageOffset startOffset, int maxRecordCount)
        {
            if (maxRecordCount < 0)
                throw new ArgumentOutOfRangeException("maxRecordCount");

            if (!AzureContainer.IsValid(Config, Container))
                yield break;

            // CHECK existence
            using (var cont = AzureContainer.OpenExistingForReading(Config, Container))
            {
                foreach (var record in cont.ReadAll(startOffset, maxRecordCount))
                {
                    yield return record;
                }
            }
        }

        public void WriteEventsInLargeBatch(string streamKey, IEnumerable<RecordForStaging> records)
        {
            var container = _blob.Container;
            container.CreateIfNotExist();

            var uri = string.Format("yyyy-MM-dd-{0}.stage",Guid.NewGuid().ToString().ToLowerInvariant());
            var tempBlob = container.GetBlockBlobReference(uri);
            try
            {
                var bytes = PrepareStaging(records);
                Log.Debug("Uploading staging to {0}", tempBlob.Uri);
                tempBlob.UploadByteArray(bytes);
                ImportEventsInternal(streamKey, uri);
            }
            finally
            {
                //tempBlob.Delete();
            }
        }

        static byte[] PrepareStaging(IEnumerable<RecordForStaging> records)
        {
            using (var stream = new MemoryStream(1024 * 1024))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (var record in records)
                    {
                        if (record.Data.Length > MessageSizeLimit)
                            throw new ArgumentException(string.Format("Messages can't be larger than {0} bytes", MessageSizeLimit));

                        writer.Write(record.Data.Length);
                        writer.Write(record.Data);
                    }
                }
                
                return stream.ToArray();
            }
        }
    }
}
