using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Platform.StreamClients;
using Platform.StreamStorage;

namespace Platform.TestClient.Commands.Bench
{
    class SmartAppBenchmarkProcessor : ICommandProcessor
    {
        int _failures;

        public string Key { get { return "SABENCH1"; } }
        public string Usage { get { return "SABENCH1 [batchcount batchsize projectioncount readercount"; } }

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            var batchCount = 15;
            var batchSize = 10000;
            var projectionCount = 10;
            var readerCount = 10;

            if (args.Length > 0)
                int.TryParse(args[0], out batchCount);
            if (args.Length > 1)
                int.TryParse(args[1], out batchSize);
            if (args.Length > 2)
                int.TryParse(args[2], out projectionCount);
            if (args.Length > 3)
                int.TryParse(args[3], out readerCount);

            context.Log.Debug("batchCount: {0}", batchCount);
            context.Log.Debug("batchSize: {0}", batchSize);
            context.Log.Debug("projectionCount: {0}", projectionCount);
            context.Log.Debug("readerCount: {0}", readerCount);

            var startEvt = new ManualResetEventSlim(false);
            _failures = 0;

            var streamId = "SmartAppTest-" + Guid.NewGuid();

            var totalSw = Stopwatch.StartNew();

            // write half of data to the stream
            var writeStat = new Stat();
            WriteEvents(streamId, batchCount / 2, batchSize, context, writeStat);
            var wrBytesPerSec = writeStat.ElapsedMsec > 0 ? (writeStat.Count * 1000D / writeStat.ElapsedMsec) : 0;
            context.Log.Debug("Events write throughput: {0}", FormatEvil.SpeedInBytes(wrBytesPerSec));

            var threads = new List<Thread>();

            using (var cts = new CancellationTokenSource())
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token))
            {
                // Upload some random data to an event stream in batches (a thread and large batches)
                writeStat = new Stat();
                var writerThread = CreateEventWriterThread(streamId, batchCount / 2, batchSize, context, writeStat, startEvt, linked.Token);
                writerThread.Start();

                // run a set of projections in parallel for this event stream (1 thread per projection)
                var projStat = new Stat[projectionCount];
                for (var i = 0; i < projectionCount; i++)
                {
                    projStat[i] = new Stat();
                    threads.Add(CreateProjectionThread(streamId, i, context, startEvt, linked.Token, projStat[i]));
                    threads.Last().Start();
                }

                // poll projected views with multiple concurrent readers
                var projectionIndex = 0;
                var readerStat = new Stat[readerCount];
                for (var i = 0; i < readerCount; i++)
                {
                    readerStat[i] = new Stat();
                    threads.Add(CreateViewReaderThread(i, projectionIndex, context, startEvt, linked.Token, readerStat[i]));
                    threads.Last().Start();

                    projectionIndex++;
                    if (projectionIndex >= projectionCount)
                        projectionIndex = 0;
                }

                // Start all thread
                startEvt.Set();

                // Wait until second half of data will be written
                writerThread.Join();

                // Cancel rest threads
                cts.Cancel();
                foreach (var thread in threads)
                    thread.Join();

                totalSw.Stop();

                // Projections stat
                wrBytesPerSec = writeStat.ElapsedMsec > 0 ? (writeStat.Count * 1000D / writeStat.ElapsedMsec) : 0;
                context.Log.Debug("Events write throughput under load: {0}", FormatEvil.SpeedInBytes(wrBytesPerSec));

                var elapsedMsec = projStat.Max(s => s.ElapsedMsec);
                var totalBytes = projStat.Sum(s => s.Count);
                var bytesPerSec = elapsedMsec > 0 ? (totalBytes * 1000D / elapsedMsec) : 0;
                context.Log.Debug("Events read throughput: {0}", FormatEvil.SpeedInBytes(bytesPerSec));

                elapsedMsec = readerStat.Max(s => s.ElapsedMsec);
                var totalReads = readerStat.Sum(s => s.Count);
                var readsPerSec = elapsedMsec > 0 ? (totalReads * 1000D / elapsedMsec) : 0;
                context.Log.Debug("Views object read rate: {0} instance/sec", FormatEvil.ToHumanReadable(readsPerSec));

                context.Log.Info("Total time: {0}", totalSw.Elapsed);

                var key = string.Format("SAB-WR-{0}-{1}-bytesPerSec", batchCount, batchSize);
                PerfUtils.LogTeamCityGraphData(key, (int) wrBytesPerSec);
                PerfUtils.LogTeamCityGraphData("SAB-RE-bytesPerSec", (int) bytesPerSec);
                PerfUtils.LogTeamCityGraphData("SAB-RV-objectsPerSec", (int) readsPerSec);
            }

            return true;
        }

        Thread CreateEventWriterThread(string streamId, int batchCount, int batchSize, CommandProcessorContext context, Stat stat, ManualResetEventSlim startEvt, CancellationToken token)
        {
            return new Thread(() => WriteEvents(streamId, batchCount, batchSize, context, stat, startEvt, token));
        }

        void WriteEvents(string streamId, int batchCount, int batchSize, CommandProcessorContext context, Stat stat,
            ManualResetEventSlim startEvt = null, CancellationToken token = default(CancellationToken))
        {
            const int msgSize = 1024;
            var rnd = new Random((int) DateTime.Now.Ticks);
            var totalBytes = 0;
            var sw = new Stopwatch();

            if (startEvt != null)
                startEvt.Wait(token);

            context.Log.Debug("Writing {0} batches", batchCount);
            sw.Start();

            var count = 0;
            while (count < batchCount && (token == default(CancellationToken) || !token.IsCancellationRequested))
            {
                try
                {
                    var records = Enumerable.Range(0, batchSize)
                        .Select(_ =>
                            new RecordForStaging(Enumerable.Range(0, msgSize).Select(b => (byte) rnd.Next()).ToArray()));
                    context.Client.Streams.WriteEventsInLargeBatch(streamId, records);
                    count++;
                    totalBytes += batchSize * msgSize;
                }
                catch (Exception e)
                {
                    Interlocked.Increment(ref _failures);
                    context.Log.ErrorException(e, "Event write error");
                    Thread.Sleep(500);
                }
            }

            sw.Stop();

            stat.Count = totalBytes;
            stat.ElapsedMsec = sw.ElapsedMilliseconds;
        }

        Thread CreateProjectionThread(string streamId, int index, CommandProcessorContext context, ManualResetEventSlim startEvt, CancellationToken token, Stat stat)
        {
            return new Thread(() =>
                {
                    var fileName = "smartapptest-" + index;

                    var views = context.Client.Views;
                    views.CreateContainer();
                    var projection = new DistributionProjection(views.ReadAsJsonOrGetNew<DistributionView>(fileName));

                    var totalBytes = 0;
                    var sw = new Stopwatch();

                    startEvt.Wait(token);
                    sw.Start();

                    while (!token.IsCancellationRequested)
                    {
                        var nextOffset = projection.NextOffsetInBytes;

                        IEnumerable<RetrievedDataRecord> records;
                        try
                        {
                            records = context.Client.Streams.ReadAll(new StorageOffset(nextOffset), 10000);
                        }
                        catch (Exception e)
                        {
                            Interlocked.Increment(ref _failures);
                            context.Log.ErrorException(e, "Projection {0}: event read error", index);
                            Thread.Sleep(500);
                            continue;
                        }

                        var emptyData = true;
                        foreach (var dataRecord in records)
                        {
                            totalBytes += dataRecord.Data.Length;

                            if (dataRecord.Key != streamId)
                            {
                                projection.NextOffsetInBytes = dataRecord.Next.OffsetInBytes;
                                continue;
                            }

                            projection.Handle(dataRecord.Data);
                            projection.NextOffsetInBytes = dataRecord.Next.OffsetInBytes;

                            emptyData = false;
                        }

                        if (emptyData)
                        {
                            Thread.Sleep(1000);
                            continue;
                        }

                        try
                        {
                            views.WriteAsJson(projection.View, fileName);
                        }
                        catch (Exception e)
                        {
                            Interlocked.Increment(ref _failures);
                            context.Log.ErrorException(e, "Projection {0}: view write error", index);
                        }
                    }

                    sw.Stop();
                    stat.Count = totalBytes;
                    stat.ElapsedMsec = sw.ElapsedMilliseconds;
                });
        }

        Thread CreateViewReaderThread(int index, int projectionIndex, CommandProcessorContext context, ManualResetEventSlim startEvt, CancellationToken token, Stat stat)
        {
            return new Thread(() =>
                {
                    var fileName = "smartapptest-" + projectionIndex;
                    var views = context.Client.Views;

                    var totalReads = 0;
                    var sw = new Stopwatch();

                    startEvt.Wait(token);
                    sw.Start();

                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            views.ReadAsJsonOrGetNew<DistributionView>(fileName);
                            totalReads++;
                        }
                        catch (Exception e)
                        {
                            Interlocked.Increment(ref _failures);
                            context.Log.ErrorException(e, "Reader {0}/proj-{1}: view read error", index, projectionIndex);
                            Thread.Sleep(500);
                        }
                    }

                    sw.Stop();
                    stat.Count = totalReads;
                    stat.ElapsedMsec = sw.ElapsedMilliseconds;
                });
        }

        public class DistributionView
        {
            public int[] Histogram { get; set; }
            public long NextOffsetInBytes { get; set; }
            public int EventsProcessed { get; set; }

            //DistributionView()
            //{
            //    Histogram = new int[256];
            //}
        }

        public class DistributionProjection
        {
            public DistributionProjection(DistributionView view)
            {
                View = view;
            }

            public DistributionView View { get; private set; }

            // Store NextOffsetInBytes in View for simplicity
            public long NextOffsetInBytes
            {
                get { return View.NextOffsetInBytes; }
                set { View.NextOffsetInBytes = value; }
            }

            public void Handle(byte[] @event)
            {
                foreach (var value in @event)
                    View.Histogram[value]++;

                View.EventsProcessed += 1;
            }
        }

        public class Stat
        {
            public int Count { get; set; }
            public long ElapsedMsec { get; set; }
        }
    }
}
