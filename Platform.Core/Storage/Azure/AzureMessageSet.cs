using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;

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

        readonly AzureMetadataCheckpoint _checkpoint;

        public AzureMessageSet(AzureStoreConfiguration config, ContainerName container)
        {
            _blob = config.GetPageBlob(container.Name + "/stream.dat");
            _pageWriter = new PageWriter(512, WriteProc);
            _blob.Container.CreateIfNotExist();
            if (!_blob.Exists())
            {
                _blob.Create(ChunkSize);
            }
            _checkpoint = AzureMetadataCheckpoint.OpenOrCreateWriteable(_blob);
            _blobContentSize = _checkpoint.Read();
            _blobSpaceSize = _blob.Properties.Length;
        }

        public void Append(string streamKey, IEnumerable<byte[]> data)
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

            _checkpoint.Write(_blobContentSize);
        }


        public void Reset()
        {
            _checkpoint.Write(0);
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

    }
}
