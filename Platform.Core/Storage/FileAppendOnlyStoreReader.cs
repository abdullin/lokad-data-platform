using System;
using System.Collections.Generic;
using System.IO;

namespace Platform.Storage
{
    public class FileAppendOnlyStoreReader : IPlatformClient
    {
        readonly string _path;

        FileStream _checkStream;
        BitReader _checkBits;
        FileStream _dataStream;
        BitReader _dataBits;
        bool _disposed;

        public FileAppendOnlyStoreReader(string name)
        {
            _path = Path.GetFullPath(name ?? "");
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

            if (!File.Exists(Path.Combine(_path, "stream.dat")))
                throw new InvalidOperationException("File stream.chk found but stream.dat file does not exist");


            using (_dataStream = new FileStream(Path.Combine(_path, "stream.dat"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (_dataBits = new BitReader(_dataStream))
                {
                    _dataStream.Seek(startOffset, SeekOrigin.Begin);


                    int count = 0;
                    while (_dataStream.Position < endOffset && count <= maxRecordCount)
                    {
                        var key = _dataBits.ReadString();
                        var length = _dataBits.Reader7BitInt();

                        if (_dataStream.Position + length > _dataStream.Length)
                            throw new InvalidOperationException("Data length is out of range.");

                        var data = _dataBits.ReadBytes(length);
                        yield return new RetrievedDataRecord(key, data, _dataStream.Position);

                        if (count == maxRecordCount)
                            break;

                        count++;
                    }
                }
            }
        }

        private long GetEndOffset()
        {
            if (!File.Exists(Path.Combine(_path, "stream.chk")))
                return 0;

            using (_checkStream = new FileStream(Path.Combine(_path, "stream.chk"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (_checkBits = new BitReader(_checkStream))
                {
                    // calculate start and end offset
                    //_checkStream.Seek(0, SeekOrigin.Begin);
                    return _checkBits.ReadInt64();
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
