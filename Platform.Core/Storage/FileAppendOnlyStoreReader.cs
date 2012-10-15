using System;
using System.Collections.Generic;
using System.IO;

namespace Platform.Storage
{
    public class FileAppendOnlyStoreReader : IPlatformClient
    {
        readonly string _path;

        readonly string _checkStreamName;
        readonly string _fileStreamName;
        public FileAppendOnlyStoreReader(string name)
        {
            _path = Path.GetFullPath(name ?? "");

            _checkStreamName = Path.Combine(_path, "stream.chk");
            _fileStreamName =Path.Combine(_path, "stream.dat");
        }

        public bool IsAzure { get { return false; } }

        public IEnumerable<RetrievedDataRecord> ReadAll(long startOffset, int maxRecordCount)
        {
            if (startOffset < 0)
                throw new ArgumentOutOfRangeException("startOffset");

            if (maxRecordCount < 0)
                throw new ArgumentOutOfRangeException("maxRecordCount");

            var endOffset = GetEndOffset();

            if (startOffset >= endOffset)
                yield break;

            
            if (!File.Exists(_fileStreamName))
                throw new InvalidOperationException("File stream.chk found but stream.dat file does not exist");

            using (var dataStream = new FileStream(_fileStreamName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var dataBits = new BitReader(dataStream))
                {
                    dataStream.Seek(startOffset, SeekOrigin.Begin);


                    int count = 0;
                    while (dataStream.Position < endOffset && count <= maxRecordCount)
                    {
                        var key = dataBits.ReadString();
                        var length = dataBits.Reader7BitInt();

                        if (dataStream.Position + length > dataStream.Length)
                            throw new InvalidOperationException("Data length is out of range.");

                        var data = dataBits.ReadBytes(length);
                        yield return new RetrievedDataRecord(key, data, dataStream.Position);

                        if (count == maxRecordCount)
                            break;

                        count++;
                    }
                }
            }
        }

        private long GetEndOffset()
        {
            
            if (!File.Exists(_checkStreamName))
                return 0;
        
            using (var checkStream = new FileStream(_checkStreamName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var checkBits = new BitReader(checkStream))
                {
                    return checkBits.ReadInt64();
                }
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
