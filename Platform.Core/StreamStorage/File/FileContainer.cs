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
    public sealed class FileContainer : IDisposable
    {
        public ContainerName Container;
        public FileMessageSet Store;
        public FileCheckpoint Checkpoint;

        public void Write(string streamKey, IEnumerable<byte[]> data)
        {
            var position = Store.Append(streamKey, data);
            Checkpoint.Write(position);
        }

        public static bool ExistsValid(string root, ContainerName container)
        {
            var folder = Path.Combine(root, container.Name);
            if (!Directory.Exists(folder))
                return false;

            var check = Path.Combine(folder, "stream.chk");
            var store = Path.Combine(folder, "stream.dat");
            return (System.IO.File.Exists(check) && System.IO.File.Exists(store));
        }

        public static FileContainer CreateNew(string root, ContainerName container)
        {
            var folder = Path.Combine(root, container.Name);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var check = FileCheckpoint.OpenOrCreateForWriting((Path.Combine(folder, "stream.chk")));
            var store = FileMessageSet.CreateNew(Path.Combine(folder, "stream.dat"));
            return new FileContainer
            {
                Container = container,
                Checkpoint = check,
                Store = store
            };
        }
        public static FileContainer OpenExistingForWriting(string root, ContainerName container)
        {
            var folder = Path.Combine(root, container.Name);
            var check = FileCheckpoint.OpenOrCreateForWriting(Path.Combine(folder, "stream.chk"));
            var store = FileMessageSet.OpenExistingForWriting(Path.Combine(folder, "stream.dat"),
                check.Read());

            return new FileContainer
            {
                Checkpoint = check,
                Container = container,
                Store = store
            };
        }


        public static FileContainer OpenForReading(string root, ContainerName container)
        {
            var folder = Path.Combine(root, container.Name);
            var check = FileCheckpoint.OpenOrCreateForReading(Path.Combine(folder, "stream.chk"));
            var store = FileMessageSet.OpenForReading(Path.Combine(folder, "stream.dat"));

            return new FileContainer
            {
                Checkpoint = check,
                Container = container,
                Store = store
            };

        }

        public IEnumerable<RetrievedDataRecord> ReadAll(StorageOffset startOffset, int maxRecordCount)
        {
            Ensure.Nonnegative(maxRecordCount, "maxRecordCount");


            var maxOffset = Checkpoint.Read();

            // nothing to read from here
            if (startOffset >= new StorageOffset(maxOffset))
                yield break;

            int recordCount = 0;
            foreach (var msg in Store.ReadAll(startOffset.OffsetInBytes, maxRecordCount))
            {
                yield return new RetrievedDataRecord(msg.StreamKey, msg.Message, new StorageOffset(msg.NextOffset));
                if (++recordCount >= maxRecordCount)
                    yield break;
                // we don't want to go above the initial water mark
                if (msg.NextOffset >= maxOffset)
                    yield break;

            }
        }


        public void Reset()
        {
            Checkpoint.Write(0);
            Store.Reset();
        }



        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            using (Checkpoint)
            using (Store)
            {
                _disposed = true;
            }
        }
    }

}