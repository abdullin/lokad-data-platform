using System;
using System.Collections.Generic;
using System.IO;

namespace Platform.Storage
{
    public class FileAppendOnlyStoreReader : IAppendOnlyStreamReader, IDisposable
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

            TryOpen();
        }

        public ReadResult ReadAll(long startOffset)
        {
            if (startOffset < 0)
                throw new ArgumentOutOfRangeException("startOffset");

            if (_disposed)
                throw new ObjectDisposedException("FileAppendOnlyStoreReader");

            if (!TryOpen())
                return new ReadResult(startOffset, DataRecord.EmptyList);

            // calculate start and end offset
            _checkStream.Seek(0, SeekOrigin.Begin);
            var endOffset = _checkBits.ReadInt64();

            if (startOffset >= endOffset)
                return new ReadResult(endOffset, DataRecord.EmptyList);

            //read data
            var records = new List<DataRecord>();
            _dataStream.Seek(startOffset, SeekOrigin.Begin);
            while (_dataStream.Position < endOffset)
            {
                var key = _dataBits.ReadString();
                var length = _dataBits.Reader7BitInt();

                if (_dataStream.Position + length > _dataStream.Length)
                    throw new InvalidOperationException("Data length is out of range.");

                var data = _dataBits.ReadBytes(length);
                records.Add(new DataRecord(key, data));
            }

            return new ReadResult(endOffset, records);
        }

        bool TryOpen()
        {
            if (!File.Exists(Path.Combine(_path, "stream.chk")))
                return false;

            _checkStream = new FileStream(Path.Combine(_path, "stream.chk"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (_checkStream.Length != 8)
                _checkStream.SetLength(8);
            _checkBits = new BitReader(_checkStream);

            if (!File.Exists(Path.Combine(_path, "stream.dat")))
                throw new InvalidOperationException("File stream.chk found but stream.dat file does not exist");

            _dataStream = new FileStream(Path.Combine(_path, "stream.dat"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _dataBits = new BitReader(_dataStream);

            return true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            var disposables = new IDisposable[]
                {
                    _checkStream,
                    _checkBits,
                    _dataStream,
                    _dataBits
                };

            _checkStream = null;
            _checkBits = null;
            _dataStream = null;
            _dataBits = null;

            var list = new List<Exception>();
            foreach (var disposable in disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception e)
                {
                    list.Add(e);
                }
            }

            _disposed = true;

            if (list.Count > 0)
                throw new AggregateException(list);
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
