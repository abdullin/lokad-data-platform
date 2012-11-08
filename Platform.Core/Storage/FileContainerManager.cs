using System;
using System.Collections.Generic;
using System.IO;

namespace Platform.Storage
{
    public class FileContainerManager : IDisposable
    {
        public sealed class ContainerWriter : IDisposable
        {
            public ContainerName Container;
            public FileMessageSet Store;
            public FileCheckpoint Checkpoint;

            public void Write(string streamKey, IEnumerable<byte[]> data)
            {
                var position = Store.Append(streamKey, data);
                Checkpoint.Write(position);
            }

            public static ContainerWriter CreateNew(string root, ContainerName container)
            {
                var folder = Path.Combine(root, container.Name);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var check = FileCheckpoint.OpenOrCreateWriteable((Path.Combine(folder, "stream.chk")));
                var store = FileMessageSet.CreateNew(Path.Combine(folder, "stream.dat"));
                return new ContainerWriter
                    {
                        Container = container,
                        Checkpoint = check,
                        Store = store
                    };
            }
            public static ContainerWriter OpenExisting(string root, ContainerName container)
            {
                var folder = Path.Combine(root, container.Name);
                var check = FileCheckpoint.OpenOrCreateWriteable(Path.Combine(folder, "stream.chk"));
                var store = FileMessageSet.OpenExistingForWriting(Path.Combine(folder, "stream.dat"),
                    check.Read());

                return new ContainerWriter
                    {
                        Checkpoint = check,
                        Container = container,
                        Store = store
                    };
            }

            public void Reset()
            {
                Checkpoint.Write(0);
                Store.Reset();
            }

            public void Close()
            {
                Checkpoint.Close();
                Store.Close();
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

        readonly IDictionary<string,ContainerWriter> _stores = new Dictionary<string, ContainerWriter>();

        readonly string _rootDirectory;

        public FileContainerManager(string rootDirectory)
        {
            if (null == rootDirectory)
                throw new ArgumentNullException("rootDirectory");
            
            _rootDirectory = rootDirectory;

            if (!Directory.Exists(rootDirectory))
                Directory.CreateDirectory(rootDirectory);

            var info = new DirectoryInfo(rootDirectory);
            foreach (var child in info.GetDirectories())
            {
                var container = ContainerName.Create(child.Name);
                var streamName = Path.Combine(rootDirectory, child.Name, "stream.dat");
                if (File.Exists(streamName))
                {
                    var writer = ContainerWriter.OpenExisting(rootDirectory, container);
                    _stores.Add(container.Name, writer);
                }
            }
        }

        public void Reset()
        {
            foreach (var store in _stores)
            {
                store.Value.Reset();
            }
        }

        public void Append(ContainerName container, string streamKey, IEnumerable<byte[]> data)
        {
            ContainerWriter value;
            if (!_stores.TryGetValue(container.Name,out value))
            {
                value = ContainerWriter.CreateNew(_rootDirectory, container);
                _stores.Add(container.Name, value);
            }
            value.Write(streamKey, data);
        }


        public void Dispose()
        {
            foreach (var writer in _stores.Values)
            {
                using(writer)
                {
                    writer.Close();
                }
            }
        }
    }
}