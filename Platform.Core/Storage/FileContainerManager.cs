using System;
using System.Collections.Generic;
using System.IO;

namespace Platform
{
    public class FileContainerManager : IDisposable
    {
        readonly IDictionary<string,FileAppendOnlyStore> _stores = new Dictionary<string, FileAppendOnlyStore>();

        readonly string _rootDirectory;

        readonly bool _isReadonly;
        public FileContainerManager(string rootDirectory, bool isReadonly)
        {
            if (null == rootDirectory)
                throw new ArgumentNullException("rootDirectory");

            _isReadonly = isReadonly;
            _rootDirectory = rootDirectory;

            if (!Directory.Exists(rootDirectory))
                Directory.CreateDirectory(rootDirectory);

            foreach (var child in Directory.GetDirectories(rootDirectory))
            {
                if (File.Exists(Path.Combine(rootDirectory, child, "stream.dat")))
                {
                    _stores.Add(child, new FileAppendOnlyStore(Path.Combine(rootDirectory,child)));
                }
            }
        }

        public void Reset()
        {
            ThrowIfReadonly();
            foreach (var store in _stores)
            {
                store.Value.Reset();
            }
        }

        void ThrowIfReadonly()
        {
            if (_isReadonly)
                throw new InvalidOperationException("This store is read-only");
        }

        public void Append(ContainerName container, string streamKey, IEnumerable<byte[]> data)
        {
            ThrowIfReadonly();

            FileAppendOnlyStore value;
            if (!_stores.TryGetValue(container.Name,out value))
            {
                value = new FileAppendOnlyStore(Path.Combine(_rootDirectory, container.Name));
                _stores.Add(container.Name, value);
            }
            value.Append(streamKey, data);
        }


        public void Dispose()
        {
            foreach (var store in _stores)
            {
                store.Value.Dispose();
            }
        }
    }
}