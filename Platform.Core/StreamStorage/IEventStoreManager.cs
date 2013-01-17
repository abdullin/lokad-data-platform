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
        /// Appends events to a given store. See Project readme for the distinction
        /// between event store Id and event stream Id
        /// </summary>
        /// <param name="storeId">Id of the event store (identifies physical location
        /// of event).</param>
        /// <param name="streamId">Id of the event stream in the store (allows
        /// to group events within a store logically).</param>
        /// <param name="eventData">Data to be persisted.</param>
        void AppendEventsToStore(EventStoreId storeId, string streamId, IEnumerable<byte[]> eventData);
    }
}