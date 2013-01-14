using System;
using System.Collections.Generic;
using System.IO;
using Platform.StreamClients;

namespace Platform.Storage
{
    public class FileContainerManager : IDisposable
    {
        readonly IDictionary<string, FileContainer> _stores = new Dictionary<string, FileContainer>();

        readonly string _rootDirectory;

        readonly ILogger Log = LogManager.GetLoggerFor<FileContainerManager>();

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
                if (ContainerName.IsValid(child.Name) != ContainerName.Rule.Valid)
                {
                    Log.Error("Skipping invalid folder {0}", child.Name);
                    continue;
                }
                var container = ContainerName.Create(child.Name);
                if (FileContainer.ExistsValid(rootDirectory, container))
                {
                    var writer = FileContainer.OpenExistingForWriting(rootDirectory, container);
                    _stores.Add(container.Name, writer);
                }
                else
                {
                    Log.Error("Skipping invalid folder {0}", child.Name);
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
            FileContainer value;
            if (!_stores.TryGetValue(container.Name, out value))
            {
                value = FileContainer.CreateNew(_rootDirectory, container);
                _stores.Add(container.Name, value);
            }
            value.Write(streamKey, data);
        }


        public void Dispose()
        {
            foreach (var writer in _stores.Values)
            {
                using (writer)
                {
                    writer.Dispose();
                }
            }
        }
    }
}