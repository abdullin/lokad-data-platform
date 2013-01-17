using System.IO;
using System.Text;
using System.Threading;

namespace Platform.TestClient.Commands
{
    public class WriteProccessor : ICommandProcessor
    {
        public string Key { get { return "WR"; } }
        public string Usage { get { return @"WR [<stream-id> <data>]
    Writes a single event to the event store"; } }
        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            var eventStreamId = "default stream";
            var data = "default-data";

            if (args.Length > 0)
            {
                if (args.Length != 2)
                {
                    context.Log.Info("Expected zero or {0} arguments",args.Length);
                    return false;
                }

                eventStreamId = args[0];

                data = args[1];
            }
            context.Client.EventStores.WriteEvent(eventStreamId, Encoding.UTF8.GetBytes(data));
            return true;
        }
    }
}