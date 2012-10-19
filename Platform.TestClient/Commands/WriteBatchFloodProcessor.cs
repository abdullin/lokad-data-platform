using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.TestClient.Commands
{
    public class WriteBatchFloodProcessor : ICommandProcessor
    {
        public string Key { get { return "WBFL"; } }
        public string Usage { get { return "WBFL [<threadCount> [<batchSize> [<repeatForEachThread>]]]"; } }

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
            long totalMs = 0;
            var bytes = Encoding.UTF8.GetBytes(streamData);

            var threads = new List<Task>();
            for (int t = 0; t < threadCount; t++)
            {
                var task = Task.Factory.StartNew(() =>
                {
                    var watch = Stopwatch.StartNew();
                    for (int i = 0; i < repeatForEachThread; i++)
                    {
                        
                        context.Client.Platform.WriteEventsInLargeBatch(streamId, Enumerable.Repeat(new RecordForStaging(bytes), batchSize));
                        
                    }

                    Interlocked.Add(ref totalMs, watch.ElapsedMilliseconds);
                    

                }, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
                threads.Add(task);
            }

            Task.WaitAll(threads.ToArray());
            //context.Completed();
            // througput
            var totalBytes = bytes.Length * threadCount * repeatForEachThread * batchSize;
            
            var key = string.Format("WB_{0}_{1}_{2}_{3}_bytesPerSec", threadCount, repeatForEachThread, batchSize,
                bytes.Length);

            var bytesPerSec = (totalBytes * 1000D / totalMs);
            context.Log.Debug("Throughput: {0}", FormatEvil.SpeedInBytes(bytesPerSec));
            PerfUtils.LogTeamCityGraphData(key, (int)bytesPerSec);
            return true;
        }
    }
}