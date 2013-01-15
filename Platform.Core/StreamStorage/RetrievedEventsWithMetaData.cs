using System.Collections.Generic;
using Platform.StreamClients;

namespace Platform.StreamStorage
{
    /// <summary>
    /// Represents an non-typed record within the event-stream.
    /// </summary>
    public struct RetrievedEventsWithMetaData
    {
        public bool IsEmpty { get { return EventData == null; } }

        public static readonly ICollection<RetrievedEventsWithMetaData> EmptyList = new RetrievedEventsWithMetaData[0];

        /// <summary>
        /// Id of the stream to which this event belongs (can be used
        /// as aggregate ID or as message serialization contract)
        /// </summary>
        public readonly string StreamId;
        
        /// <summary>
        /// Data of the record itself (to be deserialized).
        /// </summary>
        public readonly byte[] EventData;
        /// <summary>
        ///  Pointer to the next event, can be used as a continuation token
        /// or persisted locally to remember location
        /// </summary>
        public readonly EventStoreOffset Next;

        public RetrievedEventsWithMetaData(string streamId, byte[] eventData,EventStoreOffset next) 
        {
            StreamId = streamId;
            EventData = eventData;
            Next = next;
        }
    }
}