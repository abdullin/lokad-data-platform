using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using Platform.CommandLine;
using Topshelf;

namespace Platform.Node.Service
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
                    x.StartAutomatically();
                    x.Service(settings => new ServerNode());
                });
        }
    }

    public class ServerNode : ServiceControl
    {
        public bool Start(HostControl hostControl)
        {
            var nodeOptions = new NodeOptions();

            var config = ConfigurationManager.AppSettings["params"] ?? "";

            if (!CommandLineParser.Default.ParseArguments(config.Split(' '), nodeOptions))
            {
                return false;
            }

            NodeEntryPoint.StartWithOptions(nodeOptions);

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            NodeEntryPoint.RequestServiceStop();
            return true;
        }
    }
}
