using System;
using System.Collections.Generic;
using System.IO;

namespace Platform.Storage
{
    public class FileAppendOnlyStoreReader : IAppendOnlyStreamReader
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

        public IEnumerable<DataRecord> ReadAll(long startOffset, int maxRecordCount)
        {
            if (startOffset < 0)
                throw new ArgumentOutOfRangeException("startOffset");

            if (maxRecordCount < 0)
                throw new ArgumentOutOfRangeException("maxRecordCount");

            using (_checkStream = new FileStream(Path.Combine(_path, "stream.chk"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (_checkStream.Length != 8)
                    _checkStream.SetLength(8);
                using (_checkBits = new BitReader(_checkStream))
                {
                    if (!File.Exists(Path.Combine(_path, "stream.dat")))
                        throw new InvalidOperationException("File stream.chk found but stream.dat file does not exist");

                    using (_dataStream = new FileStream(Path.Combine(_path, "stream.dat"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (_dataBits = new BitReader(_dataStream))
                        {
                            // calculate start and end offset
                            _checkStream.Seek(0, SeekOrigin.Begin);
                            var endOffset = _checkBits.ReadInt64();

                            if (startOffset >= endOffset)
                                return DataRecord.EmptyList;// new ReadResult(endOffset, DataRecord.EmptyList);

                            //read data
                            var records = new List<DataRecord>();
                            _dataStream.Seek(startOffset, SeekOrigin.Begin);
                            long nextPosition = startOffset + 1;
                            while (_dataStream.Position < endOffset && _dataStream.Position < startOffset + maxRecordCount)
                            {
                                var key = _dataBits.ReadString();
                                var length = _dataBits.Reader7BitInt();

                                if (_dataStream.Position + length > _dataStream.Length)
                                    throw new InvalidOperationException("Data length is out of range.");

                                var data = _dataBits.ReadBytes(length);
                                records.Add(new DataRecord(key, data, nextPosition++));
                            }

                            return records;
                        }
                    }
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
