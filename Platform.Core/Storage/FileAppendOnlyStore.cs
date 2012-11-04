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

        FileCheckpoint _checkpoint;

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

            _checkpoint.Check(_dataStream.Position);
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
            _checkpoint = new FileCheckpoint(Path.Combine(_path, "stream.chk"));

            _dataStream = new FileStream(Path.Combine(_path, "stream.dat"), FileMode.OpenOrCreate, FileAccess.Write,
                FileShare.Read);
            var offset = _checkpoint.Offset;
            _dataStream.Seek(offset, SeekOrigin.Begin);
            _dataBits = new BitWriter(_dataStream);
        }

        void Close()
        {
            _dataStream.Close();
            _checkpoint.Close();
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