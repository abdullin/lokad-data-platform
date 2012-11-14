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

            if (!_blob.Exists())
                yield break;

            var checkpoint = AzureMetadataCheckpoint.OpenOrCreateReadable(_blob);
            var endOffset = checkpoint.Read();

            if (startOffset >= new StorageOffset(endOffset))
                yield break;

            using (var stream = _blob.OpenRead())
            using (var reader = new BinaryReader(stream))
            {
                stream.Seek(startOffset.OffsetInBytes, SeekOrigin.Begin);

                var count = 0;
                while (stream.Position < endOffset && count < maxRecordCount)
                {
                    var key = reader.ReadString();
                    var length = reader.ReadInt32();

                    if (stream.Position + length > stream.Length)
                        throw new InvalidOperationException("Data length is out of range.");

                    var data = reader.ReadBytes(length);
                    yield return new RetrievedDataRecord(key, data, new StorageOffset(stream.Position));

                    if (count == maxRecordCount)
                        break;

                    count++;
                }
            }
        }

        public void WriteEventsInLargeBatch(string streamKey, IEnumerable<RecordForStaging> records)
        {
            var container = _blob.Container;
            container.CreateIfNotExist();

            var tempBlob = container.GetBlockBlobReference(Guid.NewGuid().ToString());
            try
            {
                var bytes = PrepareStaging(records, tempBlob);
                tempBlob.UploadByteArray(bytes);

                ImportEventsInternal(streamKey, tempBlob.Uri.ToString());
            }
            finally
            {
                //tempBlob.Delete();
            }
        }

        static byte[] PrepareStaging(IEnumerable<RecordForStaging> records, CloudBlob blob)
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
