using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.TestClient.Commands
{
    public class WriteEventsFloodProcessor : ICommandProcessor
    {
        public string Key { get { return "WRFL"; } }
        public string Usage { get { return "WRFL [<Thread Count> [<Count> [<Size>]]]"; } }

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            //context.IsAsync();

            long total = 0;
            int count = 0;

            var threads = new List<Task>();

            int threadCount = 5;
            var messageCount = 1000;
            int byteSize = 0;

            
            if (args.Length > 0)
                int.TryParse(args[0], out threadCount);
            if (args.Length > 1)
                int.TryParse(args[1], out messageCount);

            if (args.Length > 2)
                int.TryParse(args[2], out byteSize);

            var bytes = Encoding.UTF8.GetBytes("This is some test message to load the server");

            if (byteSize > 0)
            {
                bytes = Enumerable
                    .Range(0, byteSize)
                    .Select(i => (byte)(i % byte.MaxValue))
                    .ToArray();
            }
            else
            {
                byteSize = bytes.Length;
            }


            var global = Stopwatch.StartNew();
            for (int t = 0; t < threadCount; t++)
            {

                var task = Task.Factory.StartNew(() =>
                    {
                        var watch = Stopwatch.StartNew();
                        for (int i = 0; i < messageCount; i++)
                        {
                            
                            context.Client.Platform.WriteEvent("name", bytes);
                        }

                        Interlocked.Add(ref total, watch.Elapsed.Ticks);
                        Interlocked.Add(ref count, messageCount);

                    }, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
                threads.Add(task);
            }
            Task.WaitAll(threads.ToArray());
            //context.Completed();
            context.Log.Info("{0} per second", count / global.Elapsed.TotalSeconds);
            PerfUtils.LogTeamCityGraphData(string.Format("{0}-{1}-{2}-{3}-reqPerSec", 
                Key, 
                threadCount, 
                messageCount, byteSize), 
                (int)(count / global.Elapsed.TotalSeconds));
            return true;
        }
    }
}