using System.Collections.Generic;
using Platform.StreamClients;

namespace Platform.StreamStorage
{
    /// <summary>
    /// Represents an non-typed record within the event-stream.
    /// </summary>
    public struct RetrievedDataRecord
    {
        public bool IsEmpty { get { return Data == null; } }

        public static readonly ICollection<RetrievedDataRecord> EmptyList = new RetrievedDataRecord[0];

        /// <summary>
        /// Intended to capture of the type of record for further object-oriented deserialization.
        /// </summary>
        public readonly string Key;
        
        /// <summary>
        /// Data of the record itself (to be deserialized).
        /// </summary>
        public readonly byte[] Data;

        public readonly StorageOffset Next;

        public RetrievedDataRecord(string key, byte[] data,StorageOffset next) 
        {
            Key = key;
            Data = data;
            Next = next;
        }
    }
}