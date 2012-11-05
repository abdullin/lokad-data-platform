using System;
using System.IO;

namespace Platform.Storage
{

    /// <summary>
    /// Tracks a given location in a single mutable file
    /// </summary>
    public sealed class FileCheckpoint : IDisposable
    {
        public long Offset { get; private set; }

        readonly FileStream _stream;
        readonly BinaryReader _reader;
        readonly BinaryWriter _writer;
        bool _disposed;
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


        public FileCheckpoint(string fullName)
        {
            var path = Path.GetDirectoryName(fullName) ?? "";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            
            var exists = File.Exists(fullName);
            _stream = new FileStream(fullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            if (_stream.Length != 8)
            {
                _stream.SetLength(8);
            }

            _reader = new BinaryReader(_stream);
            _writer = new BinaryWriter(_stream);
            Offset = exists ? ReadFile() : 0;
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

        public void Check(long position)
        {
            _stream.Seek(0, SeekOrigin.Begin);
            _writer.Write(position);
            _stream.Flush(true);
            Offset = position;
        }
    }
}