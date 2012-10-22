using System.Linq;
using System.Threading;

namespace Platform.TestClient.Commands
{
    public class UsageProcessor : ICommandProcessor
    {
        private readonly CommandProcessorCollection _commands;
        public string Key { get { return "USAGE"; } }
        public string Usage { get { return Key; } }

        public UsageProcessor(CommandProcessorCollection commands)
        {
            _commands = commands;
        }

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            var allCommands = string.Join("\n\n", _commands.RegisteredProcessors
                .OrderBy(a => a.Key.ToLowerInvariant())
                .Select(x => x.Usage));
            context.Log.Info("Available commands:\n{0}", allCommands);
            return true;
        }
    }
}