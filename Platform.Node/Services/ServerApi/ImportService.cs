using System.Threading;
using System.Threading.Tasks;
using Platform.Messages;
using ServiceStack.ServiceInterface;

namespace Platform.Node.Services.ServerApi
{

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
            var container = ContainerName.Create(request.Container);
            _publisher.Publish(new ClientMessage.ImportEvents(container, request.StreamKey, request.Location, s => token.Set()));

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
}