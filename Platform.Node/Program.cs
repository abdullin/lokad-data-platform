using Topshelf;

namespace Platform.Node
{
    class Program
    {
        static void Main()
        {
            HostFactory.Run(x =>
            {
                x.SetDescription("Lokad-DataPlatform");
                x.SetDisplayName("Lokad-DataPlatform");
                x.SetServiceName("Lokad-DataPlatform");

                x.Service(settings => new ServerNode());
            });
        }
    }
}
