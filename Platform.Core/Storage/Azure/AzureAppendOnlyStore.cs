using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.WindowsAzure.StorageClient;

namespace Platform.Storage.Azure
{
    public class AzureAppendOnlyStore
    {
        readonly CloudPageBlob _blob;
        readonly PageWriter _pageWriter;
        long _blobContentSize;
        long _blobSpaceSize;

        public AzureAppendOnlyStore(string connectionString, string container)
        {
            _blob = StorageExtensions.GetPageBlobReference(connectionString, container + "/" + "stream.dat");
            _pageWriter = new PageWriter(512, WriteProc);

            _blob.Container.CreateIfNotExist();
            if (!_blob.Exists())
            {
                _blob.Create(512);
                _blob.SetCommittedSize(0);
            }

            _blobContentSize = _blob.GetCommittedSize();
            _blobSpaceSize = _blob.Properties.Length;
        }

        public void Append(string key, IEnumerable<byte[]> data)
        {
            var recArray = data.ToArray();
            using (var stream = new MemoryStream(recArray.Sum(x => 4 + key.Length + 4 + x.Length)))
            {
                using (var writer = new BitWriter(stream))
                {
                    foreach (var record in recArray)
                    {
                        writer.Write(key);
                        writer.Write7BitInt(record.Length);
                        writer.Write(record);
                    }
                }

                var bytes = stream.ToArray();

                _pageWriter.Write(bytes);
                _pageWriter.Flush();

                _blobContentSize += bytes.Length;
                _blob.SetCommittedSize(_blobContentSize);
            }
        }

        public const long ChunkSize = 1024 * 1024 * 4;

        void WriteProc(int offset, Stream source)
        {
            if (!source.CanSeek)
                throw new InvalidOperationException("Seek must be supported by a stream.");

            

            //if (offset + length > _blobSpaceSize)
            //{
            //    var newSize = _blobSpaceSize + ChunkSize;
            //    _blob.SetLength(newSize);
            //    _blobSpaceSize = newSize;
            //}
            var length = source.Length;
            if (offset + length > _blobSpaceSize)
            {
                _blob.SetLength(offset + length);
                _blobSpaceSize = offset + length;
            }

            _blob.WritePages(source, offset);
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
