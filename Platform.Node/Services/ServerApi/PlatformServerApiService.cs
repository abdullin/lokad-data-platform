using System;
using Platform.Node.Messages;

namespace Platform.Node.Services.ServerApi
{
    /// <summary>
    /// Plugs all components together into messaging infrastructure
    /// </summary>
    public class PlatformServerApiService : IHandle<SystemMessage.Init>, IHandle<SystemMessage.StartShutdown>
    {
        readonly IPublisher _publisher;
        readonly string _url;
        ServiceStackHost _host;
        public PlatformServerApiService(IPublisher publisher, string url)
        {
            _publisher = publisher;
            _url = url;
        }

        public void Handle(SystemMessage.Init message)
        {
            _host = new ServiceStackHost(_publisher);
            _host.Init();
            try
            {
                _host.Start(_url);
            }
            catch(Exception ex)
            {
                Application.Exit(ExitCode.Error, "Failed to start Http server: " + ex.Message);
            }
        }

        public void Handle(SystemMessage.StartShutdown message)
        {

            _host.Stop();
        }
    }
}