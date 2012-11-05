using System;
using System.Collections.Generic;
using System.IO;

namespace Platform.Storage
{
    public class FileContainerManager : IDisposable
    {
        readonly IDictionary<string,FileAppendOnlyStore> _stores = new Dictionary<string, FileAppendOnlyStore>();

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
                if (File.Exists(Path.Combine(rootDirectory, child.Name, "stream.dat")))
                {
                    
                    _stores.Add(child.Name, new FileAppendOnlyStore(Path.Combine(rootDirectory,child.Name)));
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