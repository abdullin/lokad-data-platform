using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Platform.StreamClients;

namespace Platform.TestClient.Commands
{
    public class WriteBatchFloodProcessor : ICommandProcessor
    {
        public string Key { get { return "WBFL"; } }
        public string Usage { get { return "WBFL [<threadCount> [<batchSize> [<repeatForEachThread> [<msgSize>]]]]"; } }

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            int threadCount = 5;
            int batchSize = 10000;
            int repeatForEachThread = 1;
            int msgSize = 10;

            string streamId = "batch";

            if (args.Length > 0)
                int.TryParse(args[0], out threadCount);
            if (args.Length > 1)
                int.TryParse(args[1], out batchSize);
            if (args.Length > 2)
                int.TryParse(args[2], out repeatForEachThread);

            if (args.Length > 3)
                int.TryParse(args[3], out msgSize);


            
            long totalMs = 0;
            var bytes = new byte[msgSize];
            new RNGCryptoServiceProvider().GetBytes(bytes);

            var threads = new List<Task>();
            for (int t = 0; t < threadCount; t++)
            {
                var task = Task.Factory.StartNew(() =>
                {
                    var watch = Stopwatch.StartNew();
                    for (int i = 0; i < repeatForEachThread; i++)
                    {
                        
                        context.Client.Streams.WriteEventsInLargeBatch(streamId, Enumerable.Repeat(new RecordForStaging(bytes), batchSize));
                        
                    }

                    Interlocked.Add(ref totalMs, watch.ElapsedMilliseconds);
                    

                }, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
                threads.Add(task);
            }

            Task.WaitAll(threads.ToArray());
            //context.Completed();
            // througput
            var totalMessages = threadCount * repeatForEachThread * batchSize;
            var totalBytes = bytes.Length * totalMessages;
            
            var key = string.Format("WB-{0}-{1}-{2}-{3}", threadCount, repeatForEachThread, batchSize, bytes.Length);

            var bytesPerSec = (totalBytes * 1000D / totalMs);
            var msgPerSec = (1000D * totalMessages / totalMs);

            context.Log.Debug("Throughput: {0} or {1}", FormatEvil.SpeedInBytes(bytesPerSec), (int)msgPerSec);
            context.Log.Debug("Average latency {0}ms", (int)totalMs / threadCount);
            context.Log.Debug("Sent total {0} with {1}msg in {2}ms", FormatEvil.SizeInBytes(totalBytes), totalMessages, totalMs);

            PerfUtils.LogTeamCityGraphData(key + "-bytesPerSec", (int)bytesPerSec);
            PerfUtils.LogTeamCityGraphData(key + "-msgPerSec", (int)msgPerSec);
            return true;
        }
    }
}