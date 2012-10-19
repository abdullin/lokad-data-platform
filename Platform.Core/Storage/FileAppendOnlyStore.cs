#region (c) 2012 Lokad Data Platform - New BSD License 

// Copyright (c) Lokad 2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace Platform
{
    public sealed class FileAppendOnlyStore : IDisposable
    {
        readonly string _path;

        BitWriter _dataBits;
        FileStream _dataStream;

        BitWriter _checkBits;
        FileStream _checkStream;

        public FileAppendOnlyStore(string directory)
        {
            _path = directory ?? "";
            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);

            Open();
        }

        public void Dispose()
        {
            Close();
        }

        public void Append(string key, IEnumerable<byte[]> data)
        {            
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

        public void Reset()
        {
            Close();
            File.Delete(Path.Combine(_path, "stream.chk"));
            File.Delete(Path.Combine(_path, "stream.dat"));

            foreach (var name in Directory.GetFiles(_path))
            {
                File.Delete(name);
            }

            Open();
        }

        void Open()
        {
            _checkStream = new FileStream(Path.Combine(_path, "stream.chk"), FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.Read);
            if (_checkStream.Length != 8)
                _checkStream.SetLength(8);
            _checkBits = new BitWriter(_checkStream);

            var b = new byte[8];
            _checkStream.Read(b, 0, 8);

            var offset = BitConverter.ToInt64(b, 0);

            _dataStream = new FileStream(Path.Combine(_path, "stream.dat"), FileMode.OpenOrCreate, FileAccess.Write,
                FileShare.Read);
            _dataStream.Seek(offset, SeekOrigin.Begin);
            _dataBits = new BitWriter(_dataStream);
        }

        void Close()
        {
            _dataStream.Close();
            _checkStream.Close();
        }

        sealed class BitWriter : BinaryWriter
        {
            public BitWriter(Stream s) : base(s) {}

            public void Write7BitInt(int length)
            {
                Write7BitEncodedInt(length);
            }
        }
    }
}