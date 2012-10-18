using System;
using System.IO;

namespace Platform.Storage.Azure
{
    /// <summary>
    /// Helps to write data to the underlying store, which accepts only
    /// pages with specific size
    /// </summary>
    public sealed class PageWriter : IDisposable
    {
        /// <summary>
        /// Delegate that writes pages to the underlying paged store.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="source">The source.</param>
        public delegate void AppendWriterDelegate(int offset, Stream source);

        readonly int _pageSizeInBytes;
        readonly AppendWriterDelegate _writer;
        MemoryStream _pending;

        int _bytesPending;
        int _fullPagesFlushed;
        bool _disposed;

        public PageWriter(int pageSizeInBytes, AppendWriterDelegate writer)
        {
            _writer = writer;
            _pageSizeInBytes = pageSizeInBytes;
            _pending = new MemoryStream();
        }

        public void Write(byte[] buffer)
        {
            CheckNotDisposed();

            _pending.Write(buffer, 0, buffer.Length);
            _bytesPending += buffer.Length;
        }

        public void Flush()
        {
            CheckNotDisposed();

            if (_bytesPending == 0)
                return;

            var size = (int) _pending.Length;
            var padSize = (_pageSizeInBytes - size % _pageSizeInBytes) % _pageSizeInBytes;
            
            using (var stream = new MemoryStream(size + padSize))
            {
                stream.Write(_pending.ToArray(), 0, (int) _pending.Length);
                if (padSize > 0)
                    stream.Write(new byte[padSize], 0, padSize);

                stream.Position = 0;
                _writer(_fullPagesFlushed * _pageSizeInBytes, stream);
            }

            var fullPagesFlushed = size / _pageSizeInBytes;

            if (fullPagesFlushed <= 0)
                return;

            // Copy remainder to the new stream and dispose the old stream
            var newStream = new MemoryStream();
            _pending.Position = fullPagesFlushed * _pageSizeInBytes;
            _pending.CopyTo(newStream);
            _pending.Dispose();
            _pending = newStream;

            _fullPagesFlushed += fullPagesFlushed;
            _bytesPending = 0;
        }

        void CheckNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Flush();

            var t = _pending;
            _pending = null;
            _disposed = true;

            t.Dispose();
        }
    }
}
