using System;

namespace Platform.Node.Messages
{
    /// <summary>
    /// These messages are produced and consumed by the ServerAPI service.
    /// </summary>
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
            public readonly EventStoreId StoreId;
            public readonly string StreamId;
            public readonly byte[] EventData;
            
            public readonly Action<AppendEventsCompleted> Envelope;

            public AppendEvents(EventStoreId storeId, 
                string streamId, byte[] eventData,  Action<AppendEventsCompleted> envelope)
            {
                StoreId = storeId;
                StreamId = streamId;
                EventData = eventData;
                Envelope = envelope;
            }
        }

        public class ImportEvents : WriteMessage
        {
            public readonly EventStoreId StoreId;
            public readonly string StreamId;
            
            public readonly string StagingLocation;
            public readonly long Size;

            public readonly Action<ImportEventsCompleted> Envelope;
            public ImportEvents(EventStoreId storeId, 
                string streamId, string stagingLocation, long size, Action<ImportEventsCompleted> envelope)
            {
                StoreId = storeId;
                StreamId = streamId;
                StagingLocation = stagingLocation;
                Envelope = envelope;
                Size = size;
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