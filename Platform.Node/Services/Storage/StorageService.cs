using System;

namespace Platform.Node
{
    public sealed class StorageService : 
        
        IHandle<ClientMessage.AppendEvents>,
        IHandle<SystemMessage.Init>
    {
        ILogger Log = LogManager.GetLoggerFor<StorageService>();
        readonly IPublisher _publisher;

        readonly Func<IAppendOnlyStore> _func;

        IAppendOnlyStore _store;
        

        
        public StorageService(Func<IAppendOnlyStore> func, IPublisher publisher)
        {
            _func = func;
            _publisher = publisher;
        }

        public void Handle(ClientMessage.AppendEvents message)
        {
            _store.Append(message.EventStream, message.Data, message.ExpectedVersion);
            

            Log.Info("Storage service got request");
            message.Envelope(new ClientMessage.AppendEventsCompleted());
        }

        public void Handle(SystemMessage.Init message)
        {
            Log.Info("Storage starting");
            try
            {
                _store = _func();
                _publisher.Publish(new SystemMessage.StorageWriterInitializationDone());
            }
            catch (Exception ex)
            {
                Application.Exit(ExitCode.Error, "Failed to initialize store: " + ex.Message);
            }
        }
    }
}