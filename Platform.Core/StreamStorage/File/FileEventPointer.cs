using System;
using System.IO;

namespace Platform.StreamStorage.File
{
    /// <summary>
    /// Not intended to be used outside <c>Platform.Core</c> itself.
    /// 
    /// Maintains a pointer to a specific event within an event store 
    /// persisted in a single mutable file (in the filesystem).
    /// </summary>
    public sealed class FileEventPointer : IEventPointer
    {
        readonly FileStream _stream;
        readonly BinaryReader _reader;
        readonly BinaryWriter _writer;
        bool _disposed;

        readonly bool _isWriter;

        public void Dispose()
        {
            if (_disposed)
                return;

            using(_stream)
            using(_reader)
            using(_writer)
            {
                _disposed = true;
                _reader.Close();

                if (_isWriter)
                    _writer.Close();
                _stream.Close();
            }
        }

        FileEventPointer(FileStream stream, bool isWriter)
        {
            _stream = stream;
            _reader = new BinaryReader(_stream);
            if (isWriter)
            {
                _isWriter = true;
                _writer = new BinaryWriter(_stream);
            }
        }

        public static FileEventPointer OpenOrCreateForReading(string fullName)
        {
            var stream = new FileStream(fullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            if (stream.Length == 0)
                stream.SetLength(8);
            return new FileEventPointer(stream, false);
            
        }
        public static FileEventPointer OpenOrCreateForWriting(string fullName)
        {
            var stream = new FileStream(fullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            if (stream.Length == 0)
                stream.SetLength(8);
            return new FileEventPointer(stream, true);
        }

        public long Read()
        {
            _stream.Seek(0, SeekOrigin.Begin);
            return _reader.ReadInt64();
        }


        public void Write(long position)
        {
            if (!_isWriter)
                throw new NotSupportedException("This checkpoint is read-only");
            _stream.Seek(0, SeekOrigin.Begin);
            _writer.Write(position);
            _stream.Flush(true);
        }
    }
}