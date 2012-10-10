using System;
using System.Text;
using Platform.Node;
using Platform.Storage;

namespace Platform.TestClient.Commands
{
    public class ReadProcessor : ICommandProcessor
    {
        readonly IAppendOnlyStreamReader _reader;

        public ReadProcessor(IAppendOnlyStreamReader reader)
        {
            _reader = reader;
        }

        public string Key { get { return "RA"; } }
        public string Usage { get { return "RA [<from-offset>]"; } }

        public bool Execute(CommandProcessorContext context, string[] args)
        {
            var fromOffset = 0;

            if (args.Length > 0)
            {
                if (args.Length != 1)
                {
                    context.Log.Info("More arguments: {0}", args.Length);
                    return false;
                }

                int.TryParse(args[0], out fromOffset);
            }

            context.IsAsync();

            var result = _reader.ReadAll(fromOffset);
            context.Log.Info("Read {0} records{1}", result.Records.Count, result.Records.Count > 0 ? ":" : ".");

            foreach (var record in result.Records)
            {
                context.Log.Info("  stream-id: {0}, data: {0}", record.Key, Encoding.UTF8.GetString(record.Data));
            }

            context.Log.Info("Next stream offset: {0}", result.NextOffset);

            context.Completed();
            return true;
        }
    }
}
