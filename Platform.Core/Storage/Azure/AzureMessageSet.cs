using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;
using Platform.StreamClients;

namespace Platform.Storage.Azure
{
    public class AzureMessageSet 
    {
        readonly CloudPageBlob _blob;
        readonly PageWriter _pageWriter;
        long _blobContentSize;
        long _blobSpaceSize;

        public const long ChunkSize = 1024 * 1024 * 4;

        static readonly ILogger Log = LogManager.GetLoggerFor<AzureMessageSet>();

        AzureMessageSet(CloudPageBlob blob, long offset)
        {
            _blob = blob;
            _pageWriter = new PageWriter(512, WriteProc);
            _blobContentSize = offset;
            _blobSpaceSize = _blob.Properties.Length;
        }

        public static AzureMessageSet OpenExistingForWriting(CloudPageBlob blob, long offset)
        {
            return new AzureMessageSet(blob, offset);
        }

        public static AzureMessageSet CreateNewForWriting(CloudPageBlob blob)
        {
            blob.Create(ChunkSize);
            return new AzureMessageSet(blob, 0);
        }
        public static AzureMessageSet OpenExistingForReading(CloudPageBlob blob)
        {
            return new AzureMessageSet(blob, -1);
        }

        public long Append(string streamKey, IEnumerable<byte[]> data)
        {
            const int limit = 4 * 1024 * 1024 - 1024; // mind the 512 boundaries
            long writtenBytes = 0;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (var record in data)
                    {
                        var newSizeEstimate = 4 + Encoding.UTF8.GetByteCount(streamKey) + 4 + record.Length;
                        if (stream.Position + newSizeEstimate >= limit)
                        {
                            writer.Flush();
                            _pageWriter.Write(stream.ToArray(), 0, stream.Position);
                            _pageWriter.Flush();
                            writtenBytes += stream.Position;
                            stream.Seek(0, SeekOrigin.Begin);
                        }

                        writer.Write(streamKey);
                        writer.Write((int)record.Length);
                        writer.Write(record);
                    }
                    writer.Flush();
                    _pageWriter.Write(stream.ToArray(), 0, stream.Position);
                    _pageWriter.Flush();
                    writtenBytes += stream.Position;
                }
            }
            _blobContentSize += writtenBytes;

            return _blobContentSize;
        }


        public void Reset()
        {
            _pageWriter.Reset();
            _blobContentSize = 0;
        }

        void WriteProc(int offset, Stream source)
        {
            if (!source.CanSeek)
                throw new InvalidOperationException("Seek must be supported by a stream.");

            var length = source.Length;
            if (offset + length > _blobSpaceSize)
            {
                var newSize = _blobSpaceSize + ChunkSize;
                Log.Debug("Increasing chunk size to {0}", newSize);
                SetLength(_blob, newSize);
                _blobSpaceSize = newSize;
            }

            _blob.WritePages(source, offset);
        }

        public static void SetLength(CloudPageBlob blob, long newLength, int timeout = 10000)
        {
            var credentials = blob.ServiceClient.Credentials;

            var requestUri = blob.Uri;
            if (credentials.NeedsTransformUri)
                requestUri = new Uri(credentials.TransformUri(requestUri.ToString()));

            var request = BlobRequest.SetProperties(requestUri, timeout, blob.Properties, null, newLength);
            request.Timeout = timeout;

            credentials.SignRequest(request);

            using (request.GetResponse()) { }
        }

        public IEnumerable<RetrievedDataRecord> ReadAll(long startOffset, long endOffset, int maxRecordCount)
        {
            using (var stream = _blob.OpenRead())
            using (var reader = new BinaryReader(stream))
            {
                stream.Seek(startOffset, SeekOrigin.Begin);

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

    }
}
