namespace Platform.StreamStorage
{
    public struct MessageWithOffset
    {
        // legacy
        public readonly string StreamKey;
        // legacy
        public readonly long NextOffset;

        public readonly byte[] Message;
        public readonly long Offset;

        public MessageWithOffset(string streamKey, long nextOffset, byte[] message, long offset)
        {
            StreamKey = streamKey;
            Message = message;
            Offset = offset;
            NextOffset = nextOffset;
        }
    }
}