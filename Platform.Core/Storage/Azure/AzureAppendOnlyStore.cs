using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace Platform.Storage.Azure
{
    public class AzureAppendOnlyStore
    {
        readonly CloudPageBlob _blob;
        readonly PageWriter _pageWriter;
        long _blobContentSize;
        long _blobSpaceSize;

        public const long ChunkSize = 1024 * 1024 * 4;

        static readonly ILogger Log = LogManager.GetLoggerFor<AzureAppendOnlyStore>();

        public AzureAppendOnlyStore(AzureStoreConfiguration configuration)
        {
            _blob = StorageExtensions.GetPageBlobReference(configuration.ConnectionString, configuration.Container + "/" + "stream.dat");
            _pageWriter = new PageWriter(512, WriteProc);

            _blob.Container.CreateIfNotExist();
            Initialize();
        }

        public void Append(string key, IEnumerable<byte[]> data)
        {
            const int limit = 4 * 1024 * 1024 - 1024; // mind the 512 boundaries
            long writtenBytes = 0;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BitWriter(stream))
                {
                    foreach (var record in data)
                    {
                        var newSizeEstimate = 4 + Encoding.UTF8.GetByteCount(key) + 4 + record.Length;
                        if (stream.Position + newSizeEstimate >= limit)
                        {
                            _pageWriter.Write(stream.GetBuffer(),0, stream.Position);
                            _pageWriter.Flush();
                            writtenBytes += stream.Position;
                            stream.Seek(0, SeekOrigin.Begin);
                        }

                        writer.Write(key);
                        writer.Write7BitInt(record.Length);
                        writer.Write(record);
                    }
                    _pageWriter.Write(stream.GetBuffer(), 0, stream.Position);
                    _pageWriter.Flush();
                    writtenBytes += stream.Position;
                }
            }
            _blobContentSize += writtenBytes;
            _blob.SetCommittedSize(_blobContentSize);
        }


        public void Reset()
        {
            _blob.DeleteIfExists();

            _blob.Container.ListBlobs()
                .AsParallel()
                .ForAll(blob => blob.Container.GetBlobReference(blob.Uri.ToString()).DeleteIfExists());

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
            if (!_blob.Exists())
            {
                _blob.Create(ChunkSize);
                _blob.SetCommittedSize(0);
            }

            _blobContentSize = _blob.GetCommittedSize();
            _blobSpaceSize = _blob.Properties.Length;
        }

        sealed class BitWriter : BinaryWriter
        {
            public BitWriter(Stream s) : base(s)
            {
            }

            public void Write7BitInt(int length)
            {
                Write7BitEncodedInt(length);
            }
        }
    }
}
