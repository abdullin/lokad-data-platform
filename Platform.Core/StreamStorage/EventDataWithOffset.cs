namespace Platform.StreamStorage
{
    public struct EventDataWithOffset
    {
        // legacy
        public readonly string StreamId;
        // legacy
        public readonly long NextOffset;

        public readonly byte[] EventData;
        public readonly long Offset;

        public EventDataWithOffset(string streamId, long nextOffset, byte[] eventData, long offset)
        {
            StreamId = streamId;
            EventData = eventData;
            Offset = offset;
            NextOffset = nextOffset;
        }
    }
}