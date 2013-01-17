namespace Platform.StreamStorage
{
    /// <summary>
    /// Result of appending events to a chunk. It will be a failure,
    /// if we are overflowing chunk boundaries and need to write
    /// to a new one. 
    /// </summary>
    public struct ChunkAppendResult
    {
        public readonly long WrittenBytes;
        public readonly int WrittenEvents;
        public readonly long ChunkPosition;

        public ChunkAppendResult(long writtenBytes, int writtenEvents, long chunkPosition)
        {
            WrittenBytes = writtenBytes;
            WrittenEvents = writtenEvents;
            ChunkPosition = chunkPosition;
        }
    }
}