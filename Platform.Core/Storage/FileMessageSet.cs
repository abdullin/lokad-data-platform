#region (c) 2012 Lokad Data Platform - New BSD License 

// Copyright (c) Lokad 2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace Platform.Storage
{

    public struct MessageWithOffset
    {
        // legacy
        public readonly string StreamKey;
        // legacy
        public readonly long NextOffset;

        public readonly byte[] Message;
        public readonly long Offset;

        public MessageWithOffset(string streamKey, long nextOffset, byte[] message, long offset)
        {
            StreamKey = streamKey;
            Message = message;
            Offset = offset;
            NextOffset = nextOffset;
        }
    }
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
            {
                _disposed = true;
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

        public static FileMessageSet OpenForReadingOrNew(string path)
        {
            // we allow creating new file message set
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
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

        public IEnumerable<MessageWithOffset> ReadAll(long starting, int maxCount)
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
                var currentOffset = _stream.Position;
                // TODO: deal with partial reads
                var key = _reader.ReadString();
                var length = _reader.ReadInt32();
                var data = _reader.ReadBytes(length);

                
                yield return new MessageWithOffset(key, _stream.Position, data, currentOffset);

                recordCount += 1;
                if (recordCount >= maxCount)
                    yield break;

                if (currentOffset >= maxOffset)
                    yield break;
            }
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