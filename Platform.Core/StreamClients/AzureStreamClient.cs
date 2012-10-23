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
        readonly CloudPageBlob _blob;

        public AzureStreamClient(AzureStoreConfiguration config, string serverEndpoint = null)
            : base(serverEndpoint)
        {
            _blob = StorageExtensions.GetPageBlobReference(config.ConnectionString, config.Container + "/" + "stream.dat");
        }

        public IEnumerable<RetrievedDataRecord> ReadAll(StorageOffset startOffset, int maxRecordCount)
        {
            if (maxRecordCount < 0)
                throw new ArgumentOutOfRangeException("maxRecordCount");

            if (!_blob.Exists())
                yield break;

            var endOffset = _blob.GetCommittedSize();

            if (startOffset >= new StorageOffset(endOffset))
                yield break;

            using (var stream = _blob.OpenRead())
            using (var reader = new BitReader(stream))
            {
                stream.Seek(startOffset.OffsetInBytes, SeekOrigin.Begin);

                var count = 0;
                while (stream.Position < endOffset && count < maxRecordCount)
                {
                    var key = reader.ReadString();
                    var length = reader.Reader7BitInt();

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

        public void WriteEventsInLargeBatch(string streamName, IEnumerable<RecordForStaging> records)
        {
            var container = _blob.Container;
            container.CreateIfNotExist();

            var tempBlob = container.GetBlockBlobReference(Guid.NewGuid().ToString());
            try
            {
                var bytes = PrepareStaging(records, tempBlob);
                tempBlob.UploadByteArray(bytes);

                ImportEventsInternal(streamName, tempBlob.Uri.ToString());
            }
            finally
            {
                tempBlob.Delete();
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
                        writer.Write(record.Data.Length);
                        writer.Write(record.Data);

                        if (stream.Position > MessageSizeLimit)
                            throw new ArgumentException(string.Format("Messages can't be larger than {0} bytes", MessageSizeLimit));
                    }
                }
                
                return stream.ToArray();
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
    }
}
