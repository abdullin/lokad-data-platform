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

                var options = new NodeOptions();
                if (!CommandLineParser.Default.ParseArguments(param.Split(' '), options))
                    throw new Exception("Failed to parse: " + param);

                options.HttpPort = endpoint.Port;
                options.LocalHttpIp = endpoint.Address.ToString();

                // Uncomment these lines and class BlobTraceListener to write traces into blob.
                //var parts = options.StoreLocation.Split('|');
                //var account = CloudStorageAccount.Parse(parts[0]);
                //var client = account.CreateCloudBlobClient();
                //var container = client.GetContainerReference(parts[1]);
                //var blobListener = new BlobTraceListener(container.GetBlobReference("log.txt"));
                //Trace.Listeners.Add(blobListener);

                Trace.WriteLine("");
                Trace.WriteLine(string.Format("Listening endpoint http://{0}:{1}/", endpoint.Address, endpoint.Port), "Information");

                _entryPoint = NodeEntryPoint.StartWithOptions(options, i => RoleEnvironment.RequestRecycle());
            }
            catch (Exception ex)
            {
                Trace.WriteLine("OnStart:Failed " + ex, "Information");
                throw;
            }

            var result = base.OnStart();

            Trace.WriteLine("OnStart:Exit");
            return result;
        }

        public override void Run()
        {
            Trace.WriteLine("Run:Enter");
            _entryPoint.WaitForServiceToExit();
            Trace.WriteLine("Run:Exit");
        }

        public override void OnStop()
        {
            Trace.WriteLine("OnStop:Enter");
            _entryPoint.RequestServiceStop();
            var finished = _entryPoint.WaitForServiceToExit(5000);
            
            Trace.WriteLine(finished ? 
                "OnStop:Worker role shutdown completed" : "OnStop:Forcing worker role shutdown",
                "Information");

            base.OnStop();

            Trace.WriteLine("OnStop:Exit");
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

    //public class BlobTraceListener : TraceListener
    //{
    //    readonly CloudBlob _blob;

    //    public BlobTraceListener(CloudBlob blob)
    //    {
    //        _blob = blob;
    //    }

    //    [MethodImpl(MethodImplOptions.Synchronized)]
    //    public override void Write(string message)
    //    {
    //        var log = string.Empty;
    //        try
    //        {
    //            log = _blob.DownloadText();
    //        }
    //        // ReSharper disable EmptyGeneralCatchClause
    //        catch {}
    //        // ReSharper restore EmptyGeneralCatchClause

    //        log = log + message;
    //        try
    //        {
    //            _blob.UploadText(log);
    //        }
    //        // ReSharper disable EmptyGeneralCatchClause
    //        catch { }
    //        // ReSharper restore EmptyGeneralCatchClause
    //    }

    //    public override void WriteLine(string message)
    //    {
    //        Write(message + Environment.NewLine);
    //    }
    //}
}
