using System;
using System.Collections.Generic;

namespace Platform.Storage.Azure
{
    public class AzureContainerManager : IDisposable
    {
        readonly AzureStoreConfiguration _config;

        readonly IDictionary<string, AzureAppendOnlyStore> _stores = new Dictionary<string, AzureAppendOnlyStore>();

        public AzureContainerManager(AzureStoreConfiguration config)
        {
            _config = config;
        }

        public void Reset()
        {
            foreach (var store in _stores.Values)
            {
                store.Reset();
            }
        }

        public void Append(ContainerName container, string streamKey, IEnumerable<byte[]> data)
        {
            AzureAppendOnlyStore store;
            if (!_stores.TryGetValue(container.Name, out store))
            {
                store = new AzureAppendOnlyStore(_config, container);
                _stores.Add(container.Name, store);
            }
            store.Append(streamKey, data);
        }

        public void Dispose()
        {

        }
    }
}