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
        readonly bool _readOnly;

        AzureMetadataCheckpoint(CloudPageBlob blob, bool readOnly)
        {
            _readOnly = readOnly;
            _blob = blob;
            Log.Debug("Checkpoint created");
        }

        public void Write(long checkpoint)
        {
            if (_readOnly)
                throw new NotSupportedException("This checkpoint is not writeable.");
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

        public static AzureMetadataCheckpoint OpenOrCreateWriteable(CloudPageBlob blob)
        {
            if (!blob.Exists())
                throw new InvalidOperationException("Blob should exist");
            return new AzureMetadataCheckpoint(blob, readOnly:false);
        }

        public static AzureMetadataCheckpoint OpenOrCreateReadable(CloudPageBlob blob)
        {
            if (!blob.Exists())
                throw new InvalidOperationException("Blob should exist");
            return new AzureMetadataCheckpoint(blob, readOnly:true);
        }
    }
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
            var name = string.Format("{0}/{1}/stream.dat", config.Container, container.Name);
            _blob = config.GetPageBlobReference(name);
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
                _blob.SetLength(newSize);
                _blobSpaceSize = newSize;
            }

            _blob.WritePages(source, offset);
        }
    }
}
