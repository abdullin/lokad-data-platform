using System;
using System.Collections.Generic;

namespace Platform.StreamStorage
{
    /// <summary>
    /// This interface is not really needed in the codebase,
    /// but it is introduced to explicitly demonstrate the unified
    /// concept between the filesystem storage and Windows Azure storage.
    /// </summary>
    public interface IEventStoreManager : IDisposable
    {
        /// <summary>
        /// Wipes data in all loaded event stores.
        /// </summary>
        void ResetAllStores();

        /// <summary>
        /// Appends events to a given store.
        /// </summary>
        /// <param name="storeId">Id of the event store.</param>
        /// <param name="streamId">Id of the event stream in the store.</param>
        /// <param name="eventData">Data to be persisted.</param>
        void AppendEventsToStore(EventStoreId storeId, string streamId, IEnumerable<byte[]> eventData);
    }
}