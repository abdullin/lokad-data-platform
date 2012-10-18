using System;

namespace Platform.Messages
{
    public static class ClientMessage
    {

        public class RequestShutdown : Message{}
        public abstract class WriteMessage : Message {}

        //public class Shutdown : Message {}

        public class AppendEvents : WriteMessage
        {
            public readonly string EventStream;
            public readonly byte[] Data;
            
            public readonly Action<AppendEventsCompleted> Envelope;

            public AppendEvents(string eventStream, byte[] data,  Action<AppendEventsCompleted> envelope)
            {
                EventStream = eventStream;
                Data = data;
                Envelope = envelope;
            }
        }

        public class ImportEvents : WriteMessage
        {
            public readonly string EventStream;
            
            public readonly string StagingLocation;

            public readonly Action<ImportEventsCompleted> Envelope;
            public ImportEvents(string eventStream, string stagingLocation, Action<ImportEventsCompleted> envelope)
            {
                EventStream = eventStream;
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