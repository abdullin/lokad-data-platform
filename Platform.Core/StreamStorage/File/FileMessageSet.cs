#region (c) 2012 Lokad Data Platform - New BSD License 

// Copyright (c) Lokad 2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace Platform.StreamStorage.File
{
    public sealed class FileMessageSet : IDisposable
    {
        readonly BinaryWriter _writer;
        readonly FileStream _stream;
        readonly BinaryReader _reader;

        readonly bool _isMutable;

        FileMessageSet(FileStream stream, bool isMutable)
        {
            if (null == stream)
                throw new ArgumentNullException("stream");

            _isMutable = isMutable;

            _stream = stream;
            if (_isMutable)
            {
                _writer = new BinaryWriter(_stream);
            }
            _reader = new BinaryReader(_stream);
        }

        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            using(_stream)
            using (_writer)
            using (_reader)
            {
                _disposed = true;
                _stream.Close();
                if (_isMutable)
                {
                    _writer.Close();
                }
                _reader.Close();
                
            }

        }

        public static FileMessageSet OpenExistingForWriting(string path, long offset)
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            stream.Seek(offset, SeekOrigin.Begin);
            return new FileMessageSet(stream, true);
        }

        public static FileMessageSet CreateNew(string path)
        {
            var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
            return new FileMessageSet(stream, true);
        }

        public static FileMessageSet OpenForReading(string path)
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return new FileMessageSet(stream, false);
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

        public IEnumerable<EventDataWithOffset> ReadAll(long starting, int maxCount)
        {
            Ensure.Nonnegative(starting, "starting");
            Ensure.Nonnegative(maxCount, "maxCount");

            var maxOffset = _stream.Length;
            if (maxOffset <= starting)
                yield break;

            var seekResult = _stream.Seek(starting, SeekOrigin.Begin);

            if (starting != seekResult)
                throw new InvalidOperationException("Failed to reach position we seeked for");

            int recordCount = 0;
            while (true)
            {
                var recordOffset = _stream.Position;
                // TODO: deal with partial reads
                var key = _reader.ReadString();
                var length = _reader.ReadInt32();
                var data = _reader.ReadBytes(length);


                var nextOffset = _stream.Position;
                yield return new EventDataWithOffset(key, nextOffset, data, recordOffset);

                recordCount += 1;
                if (recordCount >= maxCount)
                    yield break;

                if (nextOffset >= maxOffset)
                    yield break;
            }
        } 

        public void Reset()
        {
            if (!_isMutable)
                throw new NotSupportedException("This message set is read-only");
            _stream.SetLength(0);
        }
    }
}