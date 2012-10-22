using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Platform.StreamClients;

namespace Platform.TestClient.Commands
{
    public class WriteBatchProcessor : ICommandProcessor
    {
        public string Key { get { return "IE"; } }
        public string Usage { get { return "IE [<batchSize> [<streamId> [<streamData>]]]"; } }

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            int batchSize =  10000;
            string streamId = "batch";
            string streamData = "Batch test";

            if (args.Length > 0)
                int.TryParse(args[0], out batchSize);
            if (args.Length > 1)
                streamId = args[1];
            if (args.Length > 2)
                streamData = args.Skip(2).Aggregate("", (x, y) => x + " " + y);

            var global = Stopwatch.StartNew();
            context.Client.Streams.WriteEventsInLargeBatch(streamId, Enumerable.Repeat(new RecordForStaging(Encoding.UTF8.GetBytes(streamData)), batchSize));
            context.Log.Info("{0} per second", batchSize / global.Elapsed.TotalSeconds);
            PerfUtils.LogTeamCityGraphData(string.Format("{0}-latency-ms", Key), (int)(batchSize / global.Elapsed.TotalSeconds));

            return true;
        }
    }
}