using System;
using System.IO;

namespace Platform.Storage
{
    public interface ICheckpoint : IDisposable
    {
        long Read();
        void Write(long position);
    }

    /// <summary>
    /// Tracks a given location in a single mutable file
    /// </summary>
    public sealed class FileCheckpoint : ICheckpoint
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

        FileCheckpoint(FileStream stream, bool isWriter)
        {
            _stream = stream;
            _reader = new BinaryReader(_stream);
            if (isWriter)
            {
                _isWriter = true;
                _writer = new BinaryWriter(_stream);
            }
        }

        public static FileCheckpoint OpenOrCreateForReading(string fullName)
        {
            var stream = new FileStream(fullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            if (stream.Length == 0)
                stream.SetLength(8);
            return new FileCheckpoint(stream, false);
            
        }
        public static FileCheckpoint OpenOrCreateForWriting(string fullName)
        {
            var stream = new FileStream(fullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            if (stream.Length == 0)
                stream.SetLength(8);
            return new FileCheckpoint(stream, true);
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