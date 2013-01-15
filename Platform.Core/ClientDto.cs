namespace Platform
{
    /// <summary>
    /// Messages used to communicate with the server
    /// </summary>
    public static class ClientDto
    {
        public class WriteEvent
        {
            public const string Url = "/stream/";
            public string StoreId { get; set; }
            public string StreamId { get; set; }
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
            public string StoreId { get; set; }
            public string StreamId { get; set; }
            public string BatchLocation { get; set; }
            public long Length { get; set; }
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

        public class ResetStore
        {
            public const string Url = "/reset/";
        }

        public class ResetStoreResponse
        {
            public string Result { get; set; }
            public bool Success { get; set; }
        }
    }
}