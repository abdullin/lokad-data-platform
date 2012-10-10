using System.Collections.Generic;

namespace Platform.Storage
{
    public struct DataRecord
    {
        public static readonly ICollection<DataRecord> EmptyList = new DataRecord[0];

        public readonly string Key;
        public readonly byte[] Data;

        public DataRecord(string key, byte[] data) : this()
        {
            Key = key;
            Data = data;
        }
    }
}