using System;
using System.Collections.Generic;
using System.IO;

namespace Platform.StreamStorage.File
{
    public class FileEventStoreManager : IEventStoreManager
    {
        readonly IDictionary<string, FileContainer> _stores = new Dictionary<string, FileContainer>();

        readonly string _rootDirectory;

        readonly ILogger Log = LogManager.GetLoggerFor<FileEventStoreManager>();

        public FileEventStoreManager(string rootDirectory)
        {
            if (null == rootDirectory)
                throw new ArgumentNullException("rootDirectory");

            _rootDirectory = rootDirectory;

            if (!Directory.Exists(rootDirectory))
                Directory.CreateDirectory(rootDirectory);

            var info = new DirectoryInfo(rootDirectory);
            foreach (var child in info.GetDirectories())
            {
                if (EventStoreId.IsValid(child.Name) != EventStoreId.Rule.Valid)
                {
                    Log.Error("Skipping invalid folder {0}", child.Name);
                    continue;
                }
                var container = EventStoreId.Create(child.Name);
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

        public void ResetAlEventStores()
        {
            foreach (var store in _stores)
            {
                store.Value.Reset();
            }
        }

        public void AppendEventsToStore(EventStoreId storeId, string streamId, IEnumerable<byte[]> eventData)
        {
            FileContainer value;
            if (!_stores.TryGetValue(storeId.Name, out value))
            {
                value = FileContainer.CreateNew(_rootDirectory, storeId);
                _stores.Add(storeId.Name, value);
            }
            value.Write(streamId, eventData);
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