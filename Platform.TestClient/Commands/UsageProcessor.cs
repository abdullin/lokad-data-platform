using System.Linq;
using Platform.Messages;
using ServiceStack.ServiceClient.Web;

namespace Platform.TestClient.Commands
{
    public class UsageProcessor : ICommandProcessor
    {
        private readonly CommandProcessor _commands;
        public string Key { get { return "USAGE"; } }
        public string Usage { get { return Key; } }

        public UsageProcessor(CommandProcessor commands)
        {
            _commands = commands;
        }

        public bool Execute(CommandProcessorContext context, string[] args)
        {
            var allCommands = string.Join("\n\n", _commands.RegisteredProcessors.Select(x => x.Usage.ToUpper()));
            context.Log.Info("Available commands:\n{0}", allCommands);
            return true;
        }
    }
}