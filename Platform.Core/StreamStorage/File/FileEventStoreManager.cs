using System;
using System.Collections.Generic;
using System.IO;

namespace Platform.StreamStorage.File
{
    /// <summary>
    /// Event store based on local filesystem.
    /// See documentation of the <c>IEventStoreManager</c>.
    /// </summary>
    public class FileEventStoreManager : IEventStoreManager
    {
        readonly IDictionary<string, FileEventStore> _stores = new Dictionary<string, FileEventStore>();

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
                    Log.Info("Skipping invalid folder {0} (invalid name)", child.Name);
                    continue;
                }
                var container = EventStoreId.Parse(child.Name);
                if (FileEventStore.ExistsValid(rootDirectory, container))
                {
                    var writer = FileEventStore.OpenExistingForWriting(rootDirectory, container);
                    _stores.Add(container.Name, writer);
                }
                else
                {
                    Log.Info("Skipping invalid folder {0} (can't open)", child.Name);
                }
            }
        }

        public void ResetAllStores()
        {
            foreach (var store in _stores)
            {
                store.Value.Reset();
            }
        }

        public void AppendEventsToStore(EventStoreId storeId, string streamId, IEnumerable<byte[]> eventData)
        {
            FileEventStore value;
            if (!_stores.TryGetValue(storeId.Name, out value))
            {
                value = FileEventStore.CreateNew(_rootDirectory, storeId);
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