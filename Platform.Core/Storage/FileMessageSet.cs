#region (c) 2012 Lokad Data Platform - New BSD License 

// Copyright (c) Lokad 2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace Platform.Storage
{
    public sealed class FileMessageSet : IDisposable
    {
        readonly BinaryWriter _writer;
        readonly FileStream _stream;


        public FileMessageSet(FileStream stream)
        {
            if (null == stream)
                throw new ArgumentNullException("stream");

            _stream = stream;
            _writer = new BinaryWriter(_stream);
        }

        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            using(_stream)
            using (_writer)
            {
                _disposed = true;
            }

        }

        public static FileMessageSet OpenExistingForWriting(string path, long offset)
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
            stream.Seek(offset, SeekOrigin.Begin);
            return new FileMessageSet(stream);
        }

        public static FileMessageSet CreateNew(string path)
        {
            var dataStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            return new FileMessageSet(dataStream);
        }

        public long Append(string key, IEnumerable<byte[]> data)
        {            
            foreach (var buffer in data)
            {
                _writer.Write(key);
                _writer.Write(buffer.Length);
                _writer.Write(buffer);
            }
            _stream.Flush(true);
            return _stream.Position;
        }

        public void Reset()
        {
            _stream.SetLength(0);
        }
        public void Close()
        {
            _stream.Close();
            _writer.Close();
        }
    }
}