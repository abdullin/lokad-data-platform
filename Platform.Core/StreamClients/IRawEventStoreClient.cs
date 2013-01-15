using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using Platform.StreamStorage;

namespace Platform.StreamClients
{
    [Serializable]
    public class PlatformClientException : Exception
    {
        public PlatformClientException(string message, Exception ex) : base(message, ex) {}
        public PlatformClientException(string message) : base(message) {}

    }


    /// <summary>
    /// Provides raw byte-level access to the storage and messaging of
    /// Data platform. Semantics of this interface are tightly linked to
    /// the storage implementation (for scalability and performance), which
    /// in turn are linked to the concepts of transaction logs, key-value 
    /// stores and high-performance message processing. See readme of this
    /// project for more information.
    /// </summary>
    public interface IRawEventStoreClient
    {
        /// <summary>
        /// Returns lazy enumeration over all events in a given record range, fetching
        /// stream Ids inside <see cref="RetrievedEventsWithMetaData"/>.
        /// </summary>
        IEnumerable<RetrievedEventsWithMetaData> ReadAllEvents(
            EventStoreOffset startOffset = default (EventStoreOffset),
            int maxRecordCount = int.MaxValue);
        /// <summary>
        /// Writes a single event to the storage under the given key. Use this method
        /// for high-concurrency and low latency operations
        /// </summary>
        /// <param name="streamId">Name of the stream to upload to</param>
        /// <param name="eventData">Event Data to upload</param>
        void WriteEvent(string streamId, byte[] eventData);
        /// <summary>
        /// Writes events to server in a batch by first uploading it to the staging ground
        /// (near the server) and then issuing an import request. This method has more
        /// latency but is optimized for really high throughput.
        /// </summary>
        /// <param name="streamId">Name of the stream to upload to</param>
        /// <param name="eventData">Enumeration of the events to upload (can be lazy)</param>
        void WriteEventsInLargeBatch(string streamId, IEnumerable<byte[]> eventData);
    }


    /// <summary>
    /// Points to a physical location of event inside an event store.
    /// </summary>
    /// <remarks>
    /// When switching to multi-file (chunked) event stores, this class will
    /// have to be adjusted
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct EventStoreOffset
    {
        public readonly long OffsetInBytes;

        public static readonly EventStoreOffset Zero = new EventStoreOffset(0);

        public override string ToString()
        {
            return string.Format("Offset {0}b", OffsetInBytes);
        }

        public EventStoreOffset(long offsetInBytes)
        {
            Ensure.Nonnegative(offsetInBytes, "offsetInBytes");
            OffsetInBytes = offsetInBytes;
        }

        public static   bool operator >(EventStoreOffset x , EventStoreOffset y)
        {
            return x.OffsetInBytes > y.OffsetInBytes;
        }
        public static bool operator <(EventStoreOffset x , EventStoreOffset y)
        {
            return x.OffsetInBytes < y.OffsetInBytes;
        }
        public static bool operator >= (EventStoreOffset left, EventStoreOffset right)
        {
            return left.OffsetInBytes >= right.OffsetInBytes;
        }
        public static bool operator <=(EventStoreOffset left, EventStoreOffset right)
        {
            return left.OffsetInBytes <= right.OffsetInBytes;
        }


    }
}
