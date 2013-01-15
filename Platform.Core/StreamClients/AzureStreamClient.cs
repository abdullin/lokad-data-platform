using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.WindowsAzure.StorageClient;
using Platform.StreamStorage;
using Platform.StreamStorage.Azure;

namespace Platform.StreamClients
{
    public class AzureStreamClient : JsonStreamClientBase, IRawStreamClient
    {
        public AzureStoreConfiguration Config { get; set; }
        readonly CloudPageBlob _blob;

        static readonly ILogger Log = LogManager.GetLoggerFor<AzureStreamClient>();

        public AzureStreamClient(AzureStoreConfiguration config, EventStoreName container, string serverEndpoint = null)
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

            var uri = string.Format("{0:yyyy-MM-dd}-{1}.stage",DateTime.UtcNow, Guid.NewGuid().ToString().ToLowerInvariant());
            var tempBlob = container.GetPageBlobReference(uri);
            try
            {
                Log.Debug("Uploading staging to {0}", uri);
                var size = PrepareStaging(records, tempBlob);
                ImportEventsInternal(streamKey, uri, size);
            }
            finally
            {
                //tempBlob.Delete();
            }
        }

        static long PrepareStaging(IEnumerable<RecordForStaging> records, CloudPageBlob blob)
        {
            using (var fs = AzureMessageSet.CreateNewForWriting(blob))
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
    }
}
