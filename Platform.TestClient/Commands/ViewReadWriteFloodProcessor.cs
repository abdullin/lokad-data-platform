using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Platform.TestClient.Commands
{
    public class ViewReadWriteFloodProcessor : ICommandProcessor
    {
        public string Key { get { return "VRWFL"; } }
        public string Usage { get { return Key + " [<size> <repeat> <readers>]"; } }
        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            context.Client.Views.CreateContainer();
            var counters = new ConcurrentDictionary<string, int>();

            int size = 1024;
            int repeat = 20;
            int readerCount = 5;
            if (args.Length > 0) int.TryParse(args[0], out size);
            if (args.Length > 1) int.TryParse(args[1], out repeat);
            if (args.Length> 2) int.TryParse(args[2], out readerCount);

            var readFailures = 0;
            var writeFailures = 0;

            var countdown = new CountdownEvent(1 + readerCount);

            const string viewname = "ViewReadWriteFloodProcessor";

            var threads = new List<Thread>();

            for (var i = 0; i < readerCount; i++)
            {
                var reader = new Thread(() =>
                {
                    var clientRepeat = repeat * 10;
                    for (var j = 0; j < clientRepeat; j++)
                    {
                        token.ThrowIfCancellationRequested();
                        try
                        {
                            using (var ws = context.Client.Views.Advanced.OpenRead(viewname))
                            using (var ms = new MemoryStream())
                            {
                                ws.CopyTo(ms);
                            }
                            Thread.Sleep(5);
                        }
                        catch (Exception ex)
                        {
                            counters.AddOrUpdate(ex.ToString(), s => 1, (s, i1) => i1 + 1);
                            Interlocked.Increment(ref readFailures);
                            
                            // back off on error
                            Thread.Sleep(3);
                            
                        }
                    }
                    countdown.Signal();
                })
                {
                    IsBackground = true,
                    Name = "Reader-" + i
                };
                reader.Start();
                threads.Add(reader);
            }
            var writer = new Thread(() =>
                {
                    var data = new byte[size];
                    
                    for (int i = 0; i < repeat; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        try
                        {
                            using (var ws = context.Client.Views.Advanced.OpenWrite(viewname))
                            {
                                Thread.Sleep(3);
                                ws.Write(data, 0, data.Length);
                            }
                            Thread.Sleep(11);
                        }
                        catch(Exception ex)
                        {
                            counters.AddOrUpdate(ex.ToString(), s => 1, (s, i1) => i1 + 1);
                            Interlocked.Increment(ref writeFailures);
                        }
                        
                    }
                    countdown.Signal();
                }) {IsBackground = true, Name = "Writer"};
            writer.Start();
            threads.Add(writer);


            countdown.Wait();

            var key = string.Format("{0}-{1}-{2}-{3}", Key, size, repeat, readerCount);
            if (writeFailures > 0)
            {
                PerfUtils.LogTeamCityGraphData(key + "-writeFail", writeFailures);
            }
            if (readFailures > 0)
            {
                PerfUtils.LogTeamCityGraphData(key + "-readFail", readFailures);
            }

            context.Log.Debug("Errors");

            foreach (var counter in counters)
            {
                context.Log.Debug("{0} : {1}", counter.Value, counter.Key);
            }
            return true;
        }
    }
}