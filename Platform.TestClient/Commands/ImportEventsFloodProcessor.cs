using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.TestClient.Commands
{
    public class ImportEventsFloodProcessor : ICommandProcessor
    {
        public string Key { get { return "IEFL"; } }
        public string Usage { get { return "IEFL [<threadCount> [<batchSize> [<repeatForEachThread>]]]"; } }

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            int threadCount = 5;
            int batchSize = 10000;
            int repeatForEachThread = 1;


            string streamId = "batch";
            string streamData = "Batch test";

            if (args.Length > 0)
                int.TryParse(args[0], out threadCount);
            if (args.Length > 1)
                int.TryParse(args[1], out batchSize);
            if (args.Length > 2)
                int.TryParse(args[2], out repeatForEachThread);

            var global = Stopwatch.StartNew();
            long total = 0;
            long count = 0;

            var threads = new List<Task>();
            for (int t = 0; t < threadCount; t++)
            {
                var task = Task.Factory.StartNew(() =>
                {
                    var watch = Stopwatch.StartNew();
                    for (int i = 0; i < repeatForEachThread; i++)
                    {
                        context.Client.Platform.WriteEventsInLargeBatch(streamId, Enumerable.Repeat(new RecordForStaging(Encoding.UTF8.GetBytes(streamData)), batchSize));
                    }

                    Interlocked.Add(ref total, watch.Elapsed.Ticks);
                    Interlocked.Add(ref count, batchSize * repeatForEachThread);

                }, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
                threads.Add(task);
            }

            Task.WaitAll(threads.ToArray());
            //context.Completed();
            context.Log.Info("{0} per second", count / global.Elapsed.TotalSeconds);
            PerfUtils.LogTeamCityGraphData(string.Format("{0}-latency-ms", Key), (int)(count / global.Elapsed.TotalSeconds));
            return true;
        }
    }
}