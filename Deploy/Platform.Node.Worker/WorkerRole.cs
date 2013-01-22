using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.WindowsAzure.ServiceRuntime;
using Platform.CommandLine;

namespace Platform.Node.Worker
{
    public class WorkerRole : RoleEntryPoint
    {
        NodeEntryPoint _entryPoint;
        bool _finished;

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 48;
            RoleEnvironment.Changing += RoleEnvironmentChanging;

            try
            {
                Trace.WriteLine("OnStart:System is starting up", "Information");
                var endpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["Http"].IPEndpoint;
                string param;
                if (!AzureSettingsProvider.TryGetString("params", out param))
                    param = "";

                

                //var endpointUrl = "http://" + endpoint + "/";
                Trace.WriteLine("Listening on port" + endpoint.Port, "Information");
                var options = new NodeOptions();
                if (!CommandLineParser.Default.ParseArguments(param.Split(' '), options))
                    throw new Exception("Failed to parse: " + param);

                options.HttpPort = endpoint.Port;
                options.LocalHttpIp = endpoint.Address.ToString();


                _entryPoint = NodeEntryPoint.StartWithOptions(options, i => RoleEnvironment.RequestRecycle());
            }
            catch (Exception ex)
            {
                Trace.WriteLine("OnStart:Failed " + ex, "Information");
                throw;
            }

            return base.OnStart();
        }

        public override void Run()
        {
            _entryPoint.WaitForServiceToExit();
        }

        public override void OnStop()
        {
            _entryPoint.RequestServiceStop();
            var finished = _entryPoint.WaitForServiceToExit(5000);
            
            Trace.WriteLine(finished ? 
                "OnStop:Worker role shutdown completed" : "OnStop:Forcing worker role shutdown",
                "Information");

            

            base.OnStop();
        }

        static void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            // If a configuration setting is changing
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
            {
                // restart this role instance
                e.Cancel = true;
            }
        }
    }
}
