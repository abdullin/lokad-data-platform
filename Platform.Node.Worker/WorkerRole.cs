using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Platform.Node.Worker
{
    public class WorkerRole : RoleEntryPoint
    {
        Host _host;
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
                var storageConnection = AzureSettingsProvider.GetStringOrThrow("StorageConnection");
                string container;
                if (!AzureSettingsProvider.TryGetString("StorageContainer", out container))
                    container = "dp-store";

                var endpointUrl = "http://" + endpoint + "/";
                Trace.WriteLine("Listening on " + endpointUrl, "Information");
                _host = new Host(storageConnection, container, endpointUrl);
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
            Trace.WriteLine("Run:Enter", "Information");
            try
            {
                _host.Run();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Run:Failed " + ex, "Information");
                throw;
            }

            _finished = true;
        }

        public override void OnStop()
        {
            _host.Cancel();

            var sw = Stopwatch.StartNew();
            while (!_finished && sw.ElapsedMilliseconds < 100 * 1000)
                Thread.Sleep(200);

            Trace.WriteLine(_finished ? "OnStop:Worker role shutdown completed" : "OnStop:Forcing worker role shutdown",
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
