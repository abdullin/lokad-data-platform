using System.Threading;
using Platform.Messages;
using ServiceStack.ServiceClient.Web;

namespace Platform.TestClient.Commands
{
    public class ShutdownProcessor : ICommandProcessor
    {
        public string Key { get { return "SHUTDOWN"; } }
        public string Usage { get { return Key; } }
        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            var result = new JsonServiceClient(context.Client.ClientHttpBase).Get<ClientDto.ShutdownServerResponse>("/system/shutdown/");


            return result.Success;
        }
    }
}