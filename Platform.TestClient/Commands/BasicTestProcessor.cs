#region (c) 2012 Lokad Data Platform - New BSD License

// Copyright (c) Lokad 2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Platform.StreamClients;

namespace Platform.TestClient.Commands
{
    public struct BasicTestProcessor : ICommandProcessor
    {
        public string Key
        {
            get { return "BasicTest"; }
        }

        public string Usage
        {
            get { return "BasicTest [batchcount batchsize threadcount floodsize]"; }
        }

        const string singleThreadMessageTemplate = "basic-test-one-thread-message-{0}-{{0}}";

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {

            int batchCount = 10;
            int batchSize = 10000;
            int threadCount = 10;
            int floodSize = 20;

            if (args.Length > 0)
                int.TryParse(args[0], out batchCount);
            if (args.Length > 1)
                int.TryParse(args[1], out batchSize);
            if (args.Length > 2)
                int.TryParse(args[2], out threadCount);
            if (args.Length > 3)
                int.TryParse(args[3], out floodSize);


            var total = Stopwatch.StartNew();


            string streamId = "BasicTest-" + Guid.NewGuid();

            var watch = Stopwatch.StartNew();
            var batchMessages = ImportBatch(context, streamId, batchCount, batchSize);
            var elapsedSeconds = Math.Round(watch.Elapsed.TotalSeconds, 2);
            context.Log.Debug("Imported {0}x{1} in {2}s", batchCount, batchSize, elapsedSeconds);

            watch.Restart();
            var floodMessages = FloodWrite(context,token, streamId, threadCount, floodSize);
            var round = Math.Round(watch.Elapsed.TotalSeconds, 2);
            context.Log.Debug("Flooded {0}x{1} in {2}s", threadCount, floodSize, round);

            int index = 0;
            int batchMessageCount = batchCount * batchSize;
            int floodMessagesCount = threadCount * floodSize;

            foreach (var record in context.Client.EventStores.ReadAllEvents())
            {
                var receivedMessage = Encoding.UTF8.GetString(record.EventData);

                if (!receivedMessage.StartsWith(streamId))
                    continue;

                receivedMessage = receivedMessage.Remove(0, streamId.Length);


                if (index < batchMessageCount && !batchMessages.Remove(receivedMessage))
                {
                    context.Log.Error("batch message('{0}') appears more than once", receivedMessage);
                    return false;
                }

                if (index >= batchMessageCount && !floodMessages.Remove(receivedMessage))
                {
                    context.Log.Error("flood message('{0}') appears more than once", receivedMessage);
                    return false;
                }

                index++;
            }


            if (batchMessages.Count != 0)
            {
                context.Log.Error("Batch messages: not all were able to read messages.");
                return false;
            }

            if (floodMessages.Count != 0)
            {
                context.Log.Error("Flood messages: not all were able to read messages.");
                return false;
            }

            if (index != batchMessageCount + floodMessagesCount)
            {
                context.Log.Error("not match the number of messages. Expected: {0}, Received: {1}", batchMessageCount + floodMessagesCount, index);
                return false;
            }

            var key = string.Format("BT_{0}_{1}_{2}_{3}_totalMs", batchCount, batchSize, threadCount, floodSize);
            PerfUtils.LogTeamCityGraphData(key, total.ElapsedMilliseconds);

            return true;
        }

        HashSet<string> FloodWrite(CommandProcessorContext context, CancellationToken token, string streamId, int threadCount, int floodSize)
        {
            var result = new ConcurrentBag<string>();

            
            var countdown = new CountdownEvent(threadCount);
            for (int t = 0; t < threadCount; t++)
            {
                int t1 = t;
                var task = new Thread(() =>
                    {
                        try
                        {
                            for (int i = 0; i < floodSize; i++)
                            {
                                if (token.IsCancellationRequested) return;
                                var format = string.Format("basic-test-more-thread-message-{0}-{1}", t1, i);
                                context.Client.EventStores.WriteEvent("", Encoding.UTF8.GetBytes(streamId + format));
                                result.Add(format);
                            }
                        }
                        catch (Exception ex)
                        {
                            context.Log.Error(ex.Message);
                            throw;
                        }
                        finally
                        {
                            countdown.Signal();
                        }
                    })
                    {
                        IsBackground = true,
                        Name = "BT_FloodThread_" + t
                    };
                task.Start();
            }
            countdown.Wait(token);

            return new HashSet<string>(result);
        }

        HashSet<string> ImportBatch(CommandProcessorContext context, string streamId, int batchCount, int batchSize)
        {
            var result = new HashSet<string>();

            int totalBytes = 0;
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < batchCount; i++)
            {
                string message = string.Format(singleThreadMessageTemplate, i);
                context.Client.EventStores.WriteEventsInLargeBatch("",
                    Enumerable.Range(0, batchSize).Select(
                        x =>
                        {
                            try
                            {
                                var bytes = Encoding.UTF8.GetBytes(streamId + string.Format(message, x));
                                totalBytes += bytes.Length;
                                return (bytes);
                            }
                            catch (Exception ex)
                            {
                                context.Log.Error(ex.Message);
                                throw ex;
                            }

                        }));
                for (int j = 0; j < batchSize; j++)
                {
                    result.Add(string.Format(message, j));
                }
            }

            var totalMs = watch.ElapsedMilliseconds;
            var byteSize = batchSize * batchCount > 0 ? totalBytes / (batchCount * batchSize) : 0;

            var key = string.Format("WB-{0}-{1}-{2}-{3}-bytesPerSec", 1, batchCount, batchSize, byteSize);

            var bytesPerSec = totalMs > 0 ? (totalBytes * 1000D / totalMs) : 0;
            context.Log.Debug("Throughput: {0}", FormatEvil.SpeedInBytes(bytesPerSec));
            PerfUtils.LogTeamCityGraphData(key, (int)bytesPerSec);
            return result;
        }
    }
}