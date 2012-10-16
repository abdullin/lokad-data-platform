using System;
using System.Collections.Generic;
using System.IO;

namespace Platform
{
    public sealed class FileAppendOnlyStore : IDisposable
    {
        
        readonly BitWriter _dataBits;
        readonly FileStream _dataStream;

        readonly BitWriter _checkBits;
        readonly FileStream _checkStream;


        public FileAppendOnlyStore(string directory)
        {
            var path = directory ?? "";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            _checkStream = new FileStream(Path.Combine(path, "stream.chk"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            if (_checkStream.Length != 8)
                _checkStream.SetLength(8);
            _checkBits = new BitWriter(_checkStream);


            var b = new byte[8];
            _checkStream.Read(b, 0, 8);

            var offset = BitConverter.ToInt64(b, 0);

            _dataStream = new FileStream(Path.Combine(path, "stream.dat"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            _dataStream.Seek(offset, SeekOrigin.Begin);
            _dataBits = new BitWriter(_dataStream);
        }

        public void Dispose()
        {
            _dataStream.Close();
            _checkStream.Close();
        }

        public void Append(string key, IEnumerable<byte[]> data)
        {
            //_logger.Info("Write to storage");
            // write data
            
            foreach (var buffer in data)
            {
                _dataBits.Write(key);
                _dataBits.Write7BitInt(buffer.Length);
                _dataBits.Write(buffer);
            }
            _dataStream.Flush(true);
            _checkStream.Seek(0, SeekOrigin.Begin);
            _checkBits.Write(_dataStream.Position);
            _checkStream.Flush(true);
        }

        sealed class BitWriter : BinaryWriter
        {
            public BitWriter(Stream s)  : base(s)
            {
                
            }

            public void Write7BitInt(int length)
            {
                base.Write7BitEncodedInt(length);
            }
        }
    }
}