using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace Platform.Storage.Azure
{

    public class AzureMetadataCheckpoint
    {
        readonly CloudPageBlob _blob;
        static readonly ILogger Log = LogManager.GetLoggerFor<AzureMetadataCheckpoint>();

        AzureMetadataCheckpoint(CloudPageBlob blob)
        {
            _blob = blob;
            Log.Debug("Checkpoint created");
        }

        public void Write(long checkpoint)
        {
            Log.Debug("Set checkpoint to {0}", checkpoint);
            _blob.Metadata["committedsize"] = checkpoint.ToString(CultureInfo.InvariantCulture);
            _blob.SetMetadata();
        }

        public long Read()
        {
            _blob.FetchAttributes();
            var s = _blob.Metadata["committedsize"];
            Log.Debug("Checkpoint were '{0}'", s ?? "N/A");
            var read = Int64.Parse(s ?? "0");
            return read;
        }

        public static AzureMetadataCheckpoint Attach(CloudPageBlob blob)
        {
            if (!blob.Exists())
                throw new InvalidOperationException("Blob should exist");
            return new AzureMetadataCheckpoint(blob);
        }
    }
    public class AzureAppendOnlyStore
    {
        readonly CloudPageBlob _blob;
        readonly PageWriter _pageWriter;
        long _blobContentSize;
        long _blobSpaceSize;

        public const long ChunkSize = 1024 * 1024 * 4;

        static readonly ILogger Log = LogManager.GetLoggerFor<AzureAppendOnlyStore>();

        AzureMetadataCheckpoint _checkpoint;

        public AzureAppendOnlyStore(AzureStoreConfiguration configuration, ContainerName container)
        {
            var name = string.Format("{0}/{1}/stream.dat", configuration.Container, container.Name);
            _blob = StorageExtensions.GetPageBlobReference(configuration.ConnectionString, name);
            _pageWriter = new PageWriter(512, WriteProc);
            Initialize();
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
            _blob.DeleteIfExists();

            _pageWriter.Reset();
            Initialize();
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
                _blob.SetLength(newSize);
                _blobSpaceSize = newSize;
            }

            _blob.WritePages(source, offset);
        }

        void Initialize()
        {
            _blob.Container.CreateIfNotExist();
            if (!_blob.Exists())
            {
                _blob.Create(ChunkSize);
                _checkpoint = AzureMetadataCheckpoint.Attach(_blob);
                _checkpoint.Write(0);
            }
            else
            {
                _checkpoint = AzureMetadataCheckpoint.Attach(_blob);
            }

            _blobContentSize = _checkpoint.Read();
            _blobSpaceSize = _blob.Properties.Length;
        }
    }
}
