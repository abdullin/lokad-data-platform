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
                .Add<ClientDto.WriteEvent>("/stream/", "POST")
                .Add<ClientDto.WriteEvent>("/stream/{Stream}", "POST")
                .Add<ClientDto.ImportEvents>("/import/","POST")
                .Add<ClientDto.ImportEvents>("/import/{Stream}", "POST")
                .Add<ClientDto.ShutdownServer>("/system/shutdown/", "GET");

            container.Register(_publisher);
        }
    }
}