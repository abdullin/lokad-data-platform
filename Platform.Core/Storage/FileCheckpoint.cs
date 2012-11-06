using System;
using System.IO;

namespace Platform.Storage
{

    /// <summary>
    /// Tracks a given location in a single mutable file
    /// </summary>
    public sealed class FileCheckpoint : IDisposable
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
                Close();
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

        public static FileCheckpoint OpenForReadingOrNew(string fullName)
        {
            var stream = new FileStream(fullName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
            if (stream.Length == 0)
                stream.SetLength(8);
            return new FileCheckpoint(stream, false);
        }
        public static FileCheckpoint CreateNew(string fullName)
        {
            var stream = new FileStream(fullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            stream.SetLength(8);
            return new FileCheckpoint(stream, true);
        }
        public static FileCheckpoint OpenExistingforWriting(string fullName)
        {
            var stream = new FileStream(fullName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            return new FileCheckpoint(stream, true);
        }

        public long ReadFile()
        {
            _stream.Seek(0, SeekOrigin.Begin);
            return _reader.ReadInt64();
        }

        public void Close()
        {
            _reader.Close();
            _writer.Close();
            _stream.Close();
        }

        public void Write(long position)
        {
            _stream.Seek(0, SeekOrigin.Begin);
            _writer.Write(position);
            _stream.Flush(true);
        }
    }
}