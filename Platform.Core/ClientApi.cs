namespace Platform
{
    /// <summary>
    /// ServiceStack Messages used to communicate with the Rest API of DataPlatform.
    /// </summary>
    /// <remarks>See http://www.servicestack.net/ for more details about API</remarks>
    public static class ClientApi
    {
        /// <summary>
        /// Request to write a single event
        /// </summary>
        public class WriteEvent
        {
            public const string Url = "/stream/";
            public string StoreId { get; set; }
            public string StreamId { get; set; }
            public byte[] Data { get; set; }
            public int ExpectedVersion { get; set; }
        }

        /// <summary>
        /// Result of <see cref="WriteEvent"/>
        /// </summary>
        public class WriteEventResponse
        {
            public string Result { get; set; }
            public bool Success { get; set; }
        }
        /// <summary>
        /// Request to import a single batch
        /// </summary>
        public class WriteBatch
        {
            public const string Url = "/import/";
            public string StoreId { get; set; }
            public string StreamId { get; set; }
            public string BatchLocation { get; set; }
            public long Length { get; set; }
        }
        /// <summary>
        /// Response to <see cref="WriteBatch"/>
        /// </summary>
        public class WriteBatchResponse
        {
            public string Result { get; set; }
            public bool Success { get; set; }
        }
        /// <summary>
        /// Request to shut down this server instance
        /// (for debugging and testing)
        /// </summary>
        public class ShutdownServer
        {
            public const string Url = "/system/shutdown/";
        }
        /// <summary>
        /// Response to <see cref="ShutdownServer"/>
        /// </summary>
        public class ShutdownServerResponse
        {
            public string Result { get; set; }
            public bool Success { get; set; }
        }
        /// <summary>
        /// Request to reset the entire data platform
        /// (for debugging and testing)
        /// </summary>
        public class ResetStore
        {
            public const string Url = "/reset/";
        }
        /// <summary>
        /// Responce to <see cref="ResetStoreResponse"/>
        /// </summary>
        public class ResetStoreResponse
        {
            public string Result { get; set; }
            public bool Success { get; set; }
        }
    }
}