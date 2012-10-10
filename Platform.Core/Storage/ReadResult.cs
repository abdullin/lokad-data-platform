using System.Collections.Generic;

namespace Platform.Storage
{
    public struct ReadResult
    {
        public readonly long NextOffset;
        public readonly ICollection<DataRecord> Records;

        public ReadResult(long nextOffset, ICollection<DataRecord> records) : this()
        {
            NextOffset = nextOffset;
            Records = records;
        }
    }
}