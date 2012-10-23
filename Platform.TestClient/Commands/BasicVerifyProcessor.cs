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
        public string Usage { get { return Key; } }

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            return WriteFloodAndBatchTogether(context);
        }

        bool WriteFloodAndBatchTogether(CommandProcessorContext context)
        {
            int batchCount = 0;
            int floodCount = 0;

            DateTime dt = DateTime.MaxValue;
            var errors = new ConcurrentStack<string>();
            var threads = new List<Task>();

            for (int t = 0; t < 4; t++)
            {
                var task = Task.Factory.StartNew(() =>
                {
                    while (DateTime.Now < dt)
                    {
                        try
                        {
                            context.Client.Streams.WriteEventsInLargeBatch("BasicVerify-FloodAndBatch-Write",
                            Enumerable.Range(0, 1000000).Select(
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

            for (int t = 0; t < 4; t++)
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
            dt = DateTime.Now.AddSeconds(30);
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