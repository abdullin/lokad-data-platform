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
        NodeEntryPoint _entryPoint;
        public bool Start(HostControl hostControl)
        {
            var nodeOptions = new NodeOptions();

            var config = ConfigurationManager.AppSettings["params"] ?? "";

            if (!CommandLineParser.Default.ParseArguments(config.Split(' '), nodeOptions))
            {
                return false;
            }

            _entryPoint = NodeEntryPoint.StartWithOptions(nodeOptions);

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _entryPoint.RequestServiceStop();
            return _entryPoint.WaitForServiceToExit(5000);
        }
    }
}
