using System.Text;
using System.Threading;

namespace Platform.TestClient.Commands
{
    public class WriteProccessor : ICommandProcessor
    {
        public string Key { get { return "WR"; } }
        public string Usage { get { return "WR [<stream-id> <expected-version> <data>]"; } }
        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            var eventStreamId = "default stream";
            var data = "default-data";

            if (args.Length > 0)
            {
                if (args.Length != 3)
                {
                    context.Log.Info("More arguments: {0}",args.Length);
                    return false;
                }

                eventStreamId = args[0];
                int expectedVersion;
                int.TryParse(args[1], out expectedVersion);
                data = args[2];
            }

            //context.IsAsync();

            context.Client.Streams.WriteEvent(eventStreamId, Encoding.UTF8.GetBytes(data));

            //context.Completed();
            return true;
        }
    }
}