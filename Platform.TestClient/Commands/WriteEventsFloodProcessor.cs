using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.TestClient.Commands
{
    public class WriteEventsFloodProcessor : ICommandProcessor
    {
        public string Key { get { return "WEFL"; } }
        public string Usage { get { return "WEFL [<Thread Count> [<Size>]]"; } }

        public bool Execute(CommandProcessorContext context, string[] args)
        {
            context.IsAsync();

            long total = 0;
            int count = 0;

            var threads = new List<Task>();

            int threadCount = 5;
            var size = 1000;

            if (args.Length > 0)
                int.TryParse(args[0], out threadCount);
            if (args.Length > 1)
                int.TryParse(args[1], out size);

            var global = Stopwatch.StartNew();
            for (int t = 0; t < threadCount; t++)
            {

                var task = Task.Factory.StartNew(() =>
                    {
                        var watch = Stopwatch.StartNew();
                        for (int i = 0; i < size; i++)
                        {
                            context.Client.Platform.WriteEvent("name", Encoding.UTF8.GetBytes("This is some test message to load the server"));
                        }

                        Interlocked.Add(ref total, watch.Elapsed.Ticks);
                        Interlocked.Add(ref count, size);

                    }, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
                threads.Add(task);
            }
            Task.WaitAll(threads.ToArray());
            context.Completed();
            context.Log.Info("{0} per second", count / global.Elapsed.TotalSeconds);
            PerfUtils.LogTeamCityGraphData(string.Format("{0}-latency-ms", Key), (int)(count / global.Elapsed.TotalSeconds));
            return true;
        }
    }
}