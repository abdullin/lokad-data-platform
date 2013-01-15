using System;
using System.Collections.Generic;

namespace Platform.StreamStorage
{
    /// <summary>
    /// This interface is not really needed in the codebase,
    /// but is introduced to explicitly demonstrate the concept
    /// </summary>
    public interface IEventStoreManager : IDisposable
    {
        void ResetAlEventStores();
        void AppendEventsToStore(EventStoreId storeId, string streamId, IEnumerable<byte[]> eventData);
    }
}