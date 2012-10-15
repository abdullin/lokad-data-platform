using System;

namespace Platform
{
    public static class ClientMessage
    {
        public abstract class WriteMessage : Message {}

        public class AppendEvents : WriteMessage
        {
            public readonly string EventStream;
            public readonly byte[] Data;
            public readonly int ExpectedVersion;

            public readonly Action<AppendEventsCompleted> Envelope;

            public AppendEvents(string eventStream, byte[] data, int expectedVersion, Action<AppendEventsCompleted> envelope)
            {
                EventStream = eventStream;
                Data = data;
                ExpectedVersion = expectedVersion;
                Envelope = envelope;
            }
        }

        public class ImportEvents : Message
        {
            public readonly string EventStream;
            public readonly int ExpectedVersion;
            public readonly string StagingLocation;

            public readonly Action<AppendEventsCompleted> Envelope;
            public ImportEvents(string eventStream, string stagingLocation, int expectedVersion, Action<AppendEventsCompleted> envelope)
            {
                EventStream = eventStream;
                ExpectedVersion = expectedVersion;
                StagingLocation = stagingLocation;
                Envelope = envelope;
            }
        }

        public class AppendEventsCompleted : Message
        {
            
        }

        public class ImportEventsCompleted : Message
        {
            
        }
    }
}