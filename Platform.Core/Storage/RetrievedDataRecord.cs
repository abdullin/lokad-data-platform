using System.Collections.Generic;
using Platform.StreamClients;

namespace Platform.Storage
{
    public struct RetrievedDataRecord
    {
        public bool IsEmpty { get { return Data == null; } }

        public static readonly ICollection<RetrievedDataRecord> EmptyList = new RetrievedDataRecord[0];

        public readonly string Key;
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