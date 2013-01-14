using System.Collections.Generic;

namespace Platform.StreamStorage
{
    public struct ReadResult
    {
        public readonly long NextOffset;
        public readonly ICollection<RetrievedDataRecord> Records;

        public ReadResult(long nextOffset, ICollection<RetrievedDataRecord> records) : this()
        {
            NextOffset = nextOffset;
            Records = records;
        }
    }
}