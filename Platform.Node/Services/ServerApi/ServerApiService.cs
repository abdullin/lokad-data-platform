using Platform.Messages;
using ServiceStack.Plugins.Tasks;
using ServiceStack.WebHost.Endpoints;

namespace Platform.Node.Services.ServerApi
{
    public class ServiceStackHost : AppHostHttpListenerBase
    {
        readonly IPublisher _publisher;

        public ServiceStackHost(IPublisher publisher) : base("Lokad DataPlatform API (raw)", typeof(StreamService).Assembly)
        {
            _publisher = publisher;
        }

        public override void Configure(Funq.Container container)
        {
            LoadPlugin(new TaskSupport());
            Routes
                .Add<ClientDto.WriteEvent>(ClientDto.WriteEvent.Url, "POST")
                .Add<ClientDto.WriteBatch>(ClientDto.WriteBatch.Url,"POST")
                .Add<ClientDto.ResetStore>(ClientDto.ResetStore.Url, "POST")
                .Add<ClientDto.ShutdownServer>(ClientDto.ShutdownServer.Url, "GET");

            container.Register(_publisher);
        }
    }
}