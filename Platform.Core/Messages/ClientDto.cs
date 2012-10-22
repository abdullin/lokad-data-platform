namespace Platform.Messages
{
    public static class ClientDto
    {
        public class WriteEvent
        {
            public const string Url = "/stream/";
            public string Stream { get; set; }
            public byte[] Data { get; set; }
            public int ExpectedVersion { get; set; }
        }

        public class WriteEventResponse
        {
            public string Result { get; set; }
            public bool Success { get; set; }
        }

        public class WriteBatch
        {
            public const string Url = "/import/";
            public string Stream { get; set; }
            public string Location { get; set; }
        }

        public class WriteBatchResponse
        {
            public string Result { get; set; }
            public bool Success { get; set; }
        }

        public class ShutdownServer
        {
            public const string Url = "/system/shutdown/";
        }

        public class ShutdownServerResponse
        {
            public string Result { get; set; }
            public bool Success { get; set; }
        }
    }
}