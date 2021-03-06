using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Platform.TestClient.Commands.Bench
{
    /// <summary>
    /// Helper processor which runs a set of other processors
    /// with a cleanup phase
    /// </summary>
    public class BasicBenchmarkProcessor : ICommandProcessor
    {
        public string Key { get { return "BENCH1"; } }
        public string Usage { get { return "BENCH1"; } }


        static readonly TimeSpan SlowProcessors = TimeSpan.FromSeconds(5);

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {

            var list = new BenchmarkTaskList();
            list.Add(new StartLocalServerProcessor(), "-k 300");
            list.Add(new ResetStoreProcessor(), "");
            list.Add(new WriteEventsFloodProcessor(), "5 20 44");
            list.Add(new WriteEventsFloodProcessor(), "10 10 44");
            list.Add(new WriteEventsFloodProcessor(), "5 20 600");
            list.Add(new WriteEventsFloodProcessor(), "10 10 600");
            list.Add(new WriteEventsFloodProcessor(), "1 100 44");
            list.Add(new WriteEventsFloodProcessor(), "1 100 600");

            list.Add(new BasicTestProcessor(), "10 10000 10 20");

            
            list.Add(new WriteBatchFloodProcessor(), "1 50000 5 10");
            list.Add(new WriteBatchFloodProcessor(), "5 10000 5 10");

            list.Add(new WriteBatchFloodProcessor(), "1 10000 25 10");
            list.Add(new WriteBatchFloodProcessor(), "1 10000 5 50");
            
            list.Add(new EnumerateProcessor(), "75000");
            list.Add(new ViewReadWriteFloodProcessor(), "1024 50 5");

            list.Add(new EventPointerFloodProcessor(), "200");
            try
            {
                foreach (var task in list.Tasks)
                {
                    var watch = Stopwatch.StartNew();
                    try
                    {
                        context.Log.Debug("{0} running {1} {2}", Key, task.Processor.Key, task.Args);
                        if (!task.Processor.Execute(context, token, task.GetCommandArgs()))
                        {
                            context.Log.Error("{0} failed in {1} {2}", Key, task.Processor.Key, task.Args);
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Log.ErrorException(ex, "{0} failed while running {1} {2}", Key, task.Processor.Key, task.Args);
                        context.Log.Debug(ex.ToString());
                        return false;
                    }
                    finally
                    {
                        var timeSpan = watch.Elapsed;
                        
                        if (timeSpan > SlowProcessors)
                        {
                            context.Log.Debug("{0} {1} duration was {2}s", 
                                task.Processor.Key,
                                task.Args,
                                Math.Round(timeSpan.TotalSeconds,1));
                        }
                    }
                }
                return true;
            }
            finally
            {
                new ShutdownProcessor().Execute(context, token, new string[0]);
            }
        }

        public sealed class BenchmarkTask
        {
            public readonly ICommandProcessor Processor;
            public readonly string Args;

            public BenchmarkTask(ICommandProcessor processor, string args)
            {
                Processor = processor;
                Args = args;
            }

            public string[] GetCommandArgs()
            {
                if (String.IsNullOrWhiteSpace(Args))
                    return new string[0];
                return Args.Split(' ', '\t');
            }

        }

        sealed class BenchmarkTaskList
        {
            public readonly IList<BenchmarkTask> Tasks = new List<BenchmarkTask>();

            public void Add(ICommandProcessor processor, string args)
            {
                Tasks.Add(new BenchmarkTask(processor, args));
            }
        }
    }
}