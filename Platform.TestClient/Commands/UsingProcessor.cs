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

            var isValid = ContainerName.IsValid(args[0]);
            if (isValid != ContainerName.Rule.Valid)
            {
                context.Log.Error("Container name is invalid: {0}", isValid);
                return false;
            }

            context.Log.Info("Switching to container '{0}'", args[0]);
            context.Client.UseStreamContainer(args[0]);
            
            return true;
        }
    }
}