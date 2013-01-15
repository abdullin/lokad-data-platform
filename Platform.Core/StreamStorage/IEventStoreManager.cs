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
        /// <summary>
        /// Wipes data in all loaded event stores
        /// </summary>
        void ResetAllStores();
        /// <summary>
        /// Appends events to a given store
        /// </summary>
        /// <param name="storeId">Id of the event store</param>
        /// <param name="streamId">Id of the event stream in the store</param>
        /// <param name="eventData">data to upload</param>
        void AppendEventsToStore(EventStoreId storeId, string streamId, IEnumerable<byte[]> eventData);
    }
}