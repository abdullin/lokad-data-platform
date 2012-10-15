namespace Platform.Node.Services.ServerApi
{
    public static class ClientDto
    {
        public class WriteEvent
        {
            public string Stream { get; set; }
            public byte[] Data { get; set; }
            public int ExpectedVersion { get; set; }
        }

        public class WriteEventResponse
        {
            public string Result { get; set; }
        }

        public class ImportEvents
        {
            public string Stream { get; set; }
            public string Location { get; set; }
            public int ExpectedVersion { get; set; }
        }

        public class ImportEventsResponse
        {
            public string Result { get; set; }
        }
    }
}