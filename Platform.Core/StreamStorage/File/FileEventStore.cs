using System;
using System.Collections.Generic;
using System.IO;
using Platform.StreamClients;

namespace Platform.StreamStorage.File
{
    /// <summary>
    /// Checkpointed stream stored in file system with some 
    /// specific naming conventions
    /// </summary>
    public sealed class FileEventStore : IDisposable
    {
        public readonly EventStoreId Container;
        readonly FileEventStoreChunk _store;
        readonly FileEventPointer _checkpoint;

        public FileEventStore(EventStoreId container, FileEventStoreChunk store, FileEventPointer checkpoint)
        {
            Container = container;
            _store = store;
            _checkpoint = checkpoint;
        }

        public void Write(string streamId, IEnumerable<byte[]> eventData)
        {
            var result = _store.Append(streamId, eventData);
            _checkpoint.Write(result.ChunkPosition);
        }

        public static bool ExistsValid(string root, EventStoreId container)
        {
            var folder = Path.Combine(root, container.Name);
            if (!Directory.Exists(folder))
                return false;

            var check = Path.Combine(folder, "stream.chk");
            var store = Path.Combine(folder, "stream.dat");
            return (System.IO.File.Exists(check) && System.IO.File.Exists(store));
        }

        public static FileEventStore CreateNew(string root, EventStoreId container)
        {
            var folder = Path.Combine(root, container.Name);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var check = FileEventPointer.OpenOrCreateForWriting((Path.Combine(folder, "stream.chk")));
            var store = FileEventStoreChunk.CreateNew(Path.Combine(folder, "stream.dat"));
            return new FileEventStore(container,store,check);
        }
        public static FileEventStore OpenExistingForWriting(string root, EventStoreId container)
        {
            var folder = Path.Combine(root, container.Name);
            var check = FileEventPointer.OpenOrCreateForWriting(Path.Combine(folder, "stream.chk"));
            var store = FileEventStoreChunk.OpenExistingForWriting(Path.Combine(folder, "stream.dat"), check.Read());

            return new FileEventStore(container, store, check);
        }


        public static FileEventStore OpenForReading(string root, EventStoreId container)
        {
            var folder = Path.Combine(root, container.Name);
            var check = FileEventPointer.OpenOrCreateForReading(Path.Combine(folder, "stream.chk"));
            var store = FileEventStoreChunk.OpenForReading(Path.Combine(folder, "stream.dat"));

            return new FileEventStore(container, store, check);
        }

        public IEnumerable<RetrievedEventsWithMetaData> ReadAll(EventStoreOffset startOffset, int maxRecordCount)
        {
            Ensure.Nonnegative(maxRecordCount, "maxRecordCount");


            var maxOffset = _checkpoint.Read();

            // nothing to read from here
            if (startOffset >= new EventStoreOffset(maxOffset))
                yield break;

            int recordCount = 0;
            foreach (var msg in _store.ReadAll(startOffset.OffsetInBytes, maxRecordCount))
            {
                yield return msg;
                if (++recordCount >= maxRecordCount)
                    yield break;
                // we don't want to go above the initial water mark
                if (msg.Next.OffsetInBytes >= maxOffset)
                    yield break;
            }
        }


        public void Reset()
        {
            _checkpoint.Write(0);
            _store.Reset();
        }



        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            using (_checkpoint)
            using (_store)
            {
                _disposed = true;
            }
        }
    }

}