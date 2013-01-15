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
                .Add<ClientDto.WriteEvent>(ClientDto.WriteEvent.Url, "POST")
                .Add<ClientDto.WriteBatch>(ClientDto.WriteBatch.Url, "POST")
                .Add<ClientDto.ResetStore>(ClientDto.ResetStore.Url, "POST")
                .Add<ClientDto.ShutdownServer>(ClientDto.ShutdownServer.Url, "GET");

            container.Register(_publisher);
        }
    }


    public class SystemService : ServiceBase<ClientDto.ShutdownServer>
    {
        readonly IPublisher _publisher;
        public SystemService(IPublisher publisher)
        {
            _publisher = publisher;
        }

        protected override object Run(ClientDto.ShutdownServer request)
        {
            _publisher.Publish(new ClientMessage.RequestShutdown());
            return new ClientDto.ShutdownServerResponse()
                {
                    Success = true
                };
        }
    }

    public class ResetStoreService : ServiceBase<ClientDto.ResetStore>
    {
        readonly IPublisher _publisher;

        public ResetStoreService(IPublisher publisher)
        {
            _publisher = publisher;
        }

        protected override object Run(ClientDto.ResetStore request)
        {
            var token = new ManualResetEventSlim(false);

            _publisher.Publish(new ClientMessage.RequestStoreReset(s => token.Set()));

            return Task.Factory.StartNew(() =>
                {
                    try
                    {
                        token.Wait();
                        return new ClientDto.ResetStoreResponse
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

    public class ImportService : ServiceBase<ClientDto.WriteBatch>
    {
        readonly IPublisher _publisher;

        public ImportService(IPublisher publisher)
        {
            _publisher = publisher;
        }
        protected override object Run(ClientDto.WriteBatch request)
        {
            var token = new ManualResetEventSlim(false);
            var container = EventStoreId.Create(request.StoreId);
            _publisher.Publish(new ClientMessage.ImportEvents(container, request.StreamId, request.BatchLocation, request.Length, s => token.Set()));

            return Task.Factory.StartNew(() =>
                {
                    try
                    {
                        token.Wait();
                        return new ClientDto.WriteBatchResponse()
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

    public class StreamService : ServiceBase<ClientDto.WriteEvent>
    {
        readonly IPublisher _publisher;

        public StreamService(IPublisher publisher)
        {
            _publisher = publisher;
        }


        protected override object Run(ClientDto.WriteEvent request)
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
                    return new ClientDto.WriteEventResponse()
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