#region (c) 2012 Lokad Data Platform - New BSD License 

// Copyright (c) Lokad 2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace Platform.Storage
{
    public sealed class FileAppendOnlyStore : IDisposable
    {
        readonly BinaryWriter _dataBits;
        readonly FileStream _dataStream;

        public FileAppendOnlyStore(FileStream stream, BinaryWriter writer)
        {
            if (null == stream)
                throw new ArgumentNullException("stream");
            if (null == writer)
                throw new ArgumentNullException("writer");

            _dataStream = stream;
            _dataBits = writer;
        }

        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            using(_dataStream)
            using (_dataBits)
            {
                _disposed = true;
            }

        }

        public static FileAppendOnlyStore OpenExistingForWriting(string path, long offset)
        {
            var dataStream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
            dataStream.Seek(offset, SeekOrigin.Begin);
            var dataBits = new BinaryWriter(dataStream);
            return new FileAppendOnlyStore(dataStream, dataBits);
        }

        public static FileAppendOnlyStore CreateNew(string path)
        {
            var dataStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            var dataBits = new BinaryWriter(dataStream);
            return new FileAppendOnlyStore(dataStream, dataBits);
        }

        public long Append(string key, IEnumerable<byte[]> data)
        {            
            foreach (var buffer in data)
            {
                _dataBits.Write(key);
                _dataBits.Write(buffer.Length);
                _dataBits.Write(buffer);
            }
            _dataStream.Flush(true);
            return _dataStream.Position;
        }

        public void Reset()
        {
            _dataStream.SetLength(0);
        }
        public void Close()
        {
            _dataStream.Close();
            _dataBits.Close();
        }
    }
}