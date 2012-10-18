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
    public class ImportService : ServiceBase<ClientDto.ImportEvents>
    {
        readonly IPublisher _publisher;

        public ImportService(IPublisher publisher)
        {
            _publisher = publisher;
        }
        protected override object Run(ClientDto.ImportEvents request)
        {
            var token = new ManualResetEventSlim(false);
            _publisher.Publish(new ClientMessage.ImportEvents(request.Stream, request.Location, s => token.Set()));

            return Task.Factory.StartNew(() =>
                {
                    try
                    {
                        token.Wait();
                        return new ClientDto.ImportEventsResponse()
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