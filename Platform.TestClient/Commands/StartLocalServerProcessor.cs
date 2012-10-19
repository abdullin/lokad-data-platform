using System.Diagnostics;
using System.Threading;

namespace Platform.TestClient.Commands
{
    public class StartLocalServerProcessor : ICommandProcessor
    {
        public string Key { get { return "START"; } }
        public string Usage { get { return "START [args]"; } }
        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            var file = @"..\server\Platform.Node.exe";
            var ip = context.Client.Options.Ip;
            if (ip != "localhost" && ip != "127.0.0.1")
            {
                context.Log.Error("Client IP should be localhost or 127.0.0.1. Was {0}", ip);
                return false;
            }
            var all = string.Join(" ", args);
            var proc = Process.Start(new ProcessStartInfo(file, string.Format("-h {0} {1}", context.Client.Options.HttpPort, all)));
            
            return true;
        }
    }
}