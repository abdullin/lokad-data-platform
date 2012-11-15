using System.Threading;

namespace Platform.TestClient.Commands
{
    public class UsingProcessor : ICommandProcessor
    {
        public string Key { get { return "using"; } }
        public string Usage { get { return "using <containerName>"; } }
        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {

            if (args.Length != 1)
            {
                context.Log.Error("Container name expected");
                return false;
            }
            context.Client.UseStreamContainer(args[0]);
            return true;
        }
    }
}