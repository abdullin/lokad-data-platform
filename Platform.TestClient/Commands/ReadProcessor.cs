using System.Linq;
using System.Text;
using System.Threading;
using Platform.StreamClients;

namespace Platform.TestClient.Commands
{
    public class ReadProcessor : ICommandProcessor
    {

        public string Key { get { return "RA"; } }
        public string Usage { get { return "RA [<from-offset> <max-record-count>]"; } }

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            var fromOffset = 0;
            int maxRecordCount = int.MaxValue;

            if (args.Length > 0)
            {
                if (args.Length > 2)
                {
                    context.Log.Info("More arguments: {0}", args.Length);
                    return false;
                }

                int.TryParse(args[0], out fromOffset);
                if (args.Length > 1)
                    int.TryParse(args[1], out maxRecordCount);
            }

            //context.IsAsync();

            var result = context.Client.EventStores.ReadAllEvents(new StorageOffset(fromOffset), maxRecordCount);

            StorageOffset next = StorageOffset.Zero;
            bool empty = true;
            foreach (var record in result)
            {
                context.Log.Info("  stream-id: {0}, data: {1}", record.StreamId, Encoding.UTF8.GetString(record.EventData));
                next = record.Next;
                empty = false;
            }

            var nextOffset = !empty ? next : StorageOffset.Zero;
            context.Log.Info("Next stream offset: {0}", nextOffset);

            //context.Completed();
            return true;
        }
    }
}
