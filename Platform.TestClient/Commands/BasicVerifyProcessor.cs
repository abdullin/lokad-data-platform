using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Platform.StreamClients;

namespace Platform.TestClient.Commands
{
    public class BasicVerifyProcessor : ICommandProcessor
    {
        public string Key { get { return "BV"; } }
        public string Usage { get { return "BV [TIMEOUT(Sec) BatchSize BatchThreadCount FloodThreadCount]"; } }

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            int timeOut = 30;
            int batchSize = 10000;
            int batchThreadCount = 4;
            int floodThreadCount = 4;

            if (args.Length > 0)
                int.TryParse(args[0], out timeOut);
            if (args.Length > 1)
                int.TryParse(args[1], out batchSize);
            if (args.Length > 2)
                int.TryParse(args[2], out batchThreadCount);
            if (args.Length > 3)
                int.TryParse(args[3], out floodThreadCount);

            return WriteFloodAndBatchTogether(context, timeOut, batchSize, batchThreadCount, floodThreadCount);
        }

        bool WriteFloodAndBatchTogether(CommandProcessorContext context, int timeOut, int batchSize, int batchThreadCount, int floodThreadCount)
        {
            int batchCount = 0;
            int floodCount = 0;

            DateTime dt = DateTime.MaxValue;
            var errors = new ConcurrentStack<string>();
            var threads = new List<Task>();

            for (int t = 0; t < batchThreadCount; t++)
            {
                var task = Task.Factory.StartNew(() =>
                {
                    while (DateTime.Now < dt)
                    {
                        try
                        {
                            context.Client.Streams.WriteEventsInLargeBatch("BasicVerify-FloodAndBatch-Write",
                            Enumerable.Range(0, batchSize).Select(
                                x =>
                                {
                                    var bytes = Encoding.UTF8.GetBytes("BasicVerify-FloodAndBatch-Write");
                                    return new RecordForStaging(bytes);
                                }));
                            Interlocked.Add(ref batchCount, 1);
                        }
                        catch (Exception ex)
                        {
                            errors.Push(ex.Message);
                        }

                    }
                }, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
                threads.Add(task);
            }

            for (int t = 0; t < floodThreadCount; t++)
            {
                var task = Task.Factory.StartNew(() =>
                {
                    while (DateTime.Now < dt)
                    {
                        try
                        {
                            context.Client.Streams.WriteEvent("BasicVerify-FloodAndBatch-Write", Encoding.UTF8.GetBytes("basic-test-more-thread-message"));
                            Interlocked.Add(ref floodCount, 1);
                        }
                        catch (Exception ex)
                        {
                            errors.Push(ex.Message);
                        }
                    }
                }, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
                threads.Add(task);
            }
            dt = DateTime.Now.AddSeconds(timeOut);
            Task.WaitAll(threads.ToArray());

            foreach (var err in errors.ToArray())
            {
                Console.WriteLine(err);
            }

            Console.WriteLine("Add {0} flood messages", floodCount);
            Console.WriteLine("Add {0} batch", batchCount);

            return errors.Count == 0;
        }
    }
}