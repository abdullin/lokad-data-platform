using System.Threading;
using System.Threading.Tasks;
using Platform.Node.Messages;
using ServiceStack.Plugins.Tasks;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace Platform.Node.Services.ServerApi
{
    /// <summary>
    /// This host wires together all implementations of REST services 
    /// which are based on ServiceStack lib
    /// </summary>
    public class ServiceStackHost : AppHostHttpListenerBase
    {
        readonly IPublisher _publisher;

        public ServiceStackHost(IPublisher publisher)
            : base("Lokad DataPlatform API (raw)", typeof(StreamService).Assembly)
        {
            _publisher = publisher;
        }

        public override void Configure(Funq.Container container)
        {
            LoadPlugin(new TaskSupport());
            Routes
                .Add<ClientApi.WriteEvent>(ClientApi.WriteEvent.Url, "POST")
                .Add<ClientApi.WriteBatch>(ClientApi.WriteBatch.Url, "POST")
                .Add<ClientApi.ResetStore>(ClientApi.ResetStore.Url, "POST")
                .Add<ClientApi.ShutdownServer>(ClientApi.ShutdownServer.Url, "GET");

            container.Register(_publisher);
        }
    }


    public class SystemService : ServiceBase<ClientApi.ShutdownServer>
    {
        readonly IPublisher _publisher;
        public SystemService(IPublisher publisher)
        {
            _publisher = publisher;
        }

        protected override object Run(ClientApi.ShutdownServer request)
        {
            _publisher.Publish(new ClientMessage.RequestShutdown());
            return new ClientApi.ShutdownServerResponse()
                {
                    Success = true
                };
        }
    }

    public class ResetStoreService : ServiceBase<ClientApi.ResetStore>
    {
        readonly IPublisher _publisher;

        public ResetStoreService(IPublisher publisher)
        {
            _publisher = publisher;
        }

        protected override object Run(ClientApi.ResetStore request)
        {
            var token = new ManualResetEventSlim(false);

            _publisher.Publish(new ClientMessage.RequestStoreReset(s => token.Set()));

            return Task.Factory.StartNew(() =>
                {
                    try
                    {
                        token.Wait();
                        return new ClientApi.ResetStoreResponse
                            {
                                Result = "Completed",
                                Success = true
                            };
                    }
                    finally
                    {
                        token.Dispose();
                    }
                });
        }
    }

    public class ImportService : ServiceBase<ClientApi.WriteBatch>
    {
        readonly IPublisher _publisher;

        public ImportService(IPublisher publisher)
        {
            _publisher = publisher;
        }
        protected override object Run(ClientApi.WriteBatch request)
        {
            var token = new ManualResetEventSlim(false);
            var container = EventStoreId.Create(request.StoreId);
            _publisher.Publish(new ClientMessage.ImportEvents(container, request.StreamId, request.BatchLocation, request.Length, s => token.Set()));

            return Task.Factory.StartNew(() =>
                {
                    try
                    {
                        token.Wait();
                        return new ClientApi.WriteBatchResponse()
                            {
                                Result = "Completed",
                                Success = true
                            };
                    }
                    finally
                    {
                        token.Dispose();
                    }
                });
        }
    }

    public class StreamService : ServiceBase<ClientApi.WriteEvent>
    {
        readonly IPublisher _publisher;

        public StreamService(IPublisher publisher)
        {
            _publisher = publisher;
        }


        protected override object Run(ClientApi.WriteEvent request)
        {
            var token = new ManualResetEventSlim(false);
            var name = EventStoreId.Create(request.StoreId);

            _publisher.Publish(new ClientMessage.AppendEvents(
                name,
                request.StreamId,
                request.Data, s => token.Set()));

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    token.Wait();
                    return new ClientApi.WriteEventResponse()
                    {
                        Result = "Completed",
                        Success = true
                    };
                }
                finally
                {
                    token.Dispose();
                }
            });
        }
    }

}