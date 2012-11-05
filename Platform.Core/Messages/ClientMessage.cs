using System;

namespace Platform.Messages
{
    public static class ClientMessage
    {

        public class RequestShutdown : Message{}
        public abstract class WriteMessage : Message {}

        public class RequestStoreReset : WriteMessage
        {
            public readonly Action<StoreResetCompleted> Envelope;
            public RequestStoreReset(Action<StoreResetCompleted> envelope)
            {
                Envelope = envelope;
            }
        }

        public class StoreResetCompleted : Message
        {
            
        }

        //public class Shutdown : Message {}

        public class AppendEvents : WriteMessage
        {
            public readonly string StreamKey;
            public readonly byte[] Data;
            
            public readonly Action<AppendEventsCompleted> Envelope;

            public AppendEvents(string streamKey, byte[] data,  Action<AppendEventsCompleted> envelope)
            {
                StreamKey = streamKey;
                Data = data;
                Envelope = envelope;
            }
        }

        public class ImportEvents : WriteMessage
        {
            public readonly string StreamKey;
            
            public readonly string StagingLocation;

            public readonly Action<ImportEventsCompleted> Envelope;
            public ImportEvents(string streamKey, string stagingLocation, Action<ImportEventsCompleted> envelope)
            {
                StreamKey = streamKey;
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