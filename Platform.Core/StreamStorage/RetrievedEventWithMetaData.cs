using System.Collections.Generic;
using Platform.StreamClients;

namespace Platform.StreamStorage
{
    /// <summary>
    /// Represents an non-typed record within the event-stream.
    /// </summary>
    public struct RetrievedEventWithMetaData
    {
        public bool IsEmpty { get { return EventData == null; } }

        public static readonly ICollection<RetrievedEventWithMetaData> EmptyList = new RetrievedEventWithMetaData[0];

        /// <summary>
        /// Name of the stream to which this event belongs
        /// </summary>
        public readonly string StreamName;
        
        /// <summary>
        /// Data of the record itself (to be deserialized).
        /// </summary>
        public readonly byte[] EventData;

        public readonly StorageOffset Next;

        public RetrievedEventWithMetaData(string streamName, byte[] eventData,StorageOffset next) 
        {
            StreamName = streamName;
            EventData = eventData;
            Next = next;
        }
    }
}