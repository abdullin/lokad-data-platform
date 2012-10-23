using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Platform.Storage;
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
            int batchSize = 1000;
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

            return WriteFloodAndBatchTogether(context, timeOut, batchSize, batchThreadCount, floodThreadCount) |
                   ReadMessageWithNextoffset(context);

        }

        #region Write and Read Flood/Batch messages

        bool WriteFloodAndBatchTogether(CommandProcessorContext context, int timeOut, int batchSize, int batchThreadCount, int floodThreadCount)
        {
            string streamId = Guid.NewGuid().ToString();
            const string batchMsg = "BasicVerify-Batch-Test-Message";
            const string floodMsg = "BasicVerify-Flood-Test-Message";
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
                            context.Client.Streams.WriteEventsInLargeBatch(streamId,
                            Enumerable.Range(0, batchSize).Select(
                                x =>
                                {
                                    var bytes = Encoding.UTF8.GetBytes(batchMsg);
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
                            context.Client.Streams.WriteEvent(streamId, Encoding.UTF8.GetBytes(floodMsg));
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



            Console.WriteLine("Add {0} flood messages", floodCount);
            Console.WriteLine("Add {0} batch", batchCount);

            return ReadAddMessages(context, streamId, batchMsg, floodMsg, errors, batchCount * batchSize, floodCount);
        }

        private static bool ReadAddMessages(CommandProcessorContext context, string streamId, string batchMsg, string floodMsg,
                                            ConcurrentStack<string> errors, int batchCount, int floodCount)
        {
            var records = context.Client.Streams.ReadAll().Where(x => x.Key == streamId);
            foreach (var record in records)
            {
                var msg = Encoding.UTF8.GetString(record.Data);
                if (msg.Equals(batchMsg))
                    batchCount--;
                else if (msg.Equals(floodMsg))
                    floodCount--;
                else
                    errors.Push("strange message: " + msg);
            }

            if (batchCount != 0)
                errors.Push("Unread " + batchCount + " batch messages");
            if (floodCount != 0)
                errors.Push("Unread " + floodCount + " flood messages");

            foreach (var err in errors.ToArray())
            {
                Console.WriteLine(err);
            }

            return errors.Count == 0;
        }

        #endregion

        #region Read messages

        private bool ReadMessageWithNextoffset(CommandProcessorContext context)
        {
            var result = true;
            var records = context.Client.Streams.ReadAll(maxRecordCount: 100);
            RetrievedDataRecord prevRecord = default(RetrievedDataRecord);
            bool firstRecord = true;
            foreach (var record in records)
            {
                if (firstRecord)
                {
                    firstRecord = false;
                }
                else
                {
                    var prevNextRecord = context.Client.Streams.ReadAll(prevRecord.Next, 1).First();
                    var expectedBytes = record.Data.Except(prevNextRecord.Data).ToList();
                    if (record.Key != prevNextRecord.Key | expectedBytes.Count != 0)
                    {
                        Console.WriteLine("Expected key: {0}, Received key: {1}", record.Key, prevNextRecord.Key);
                        Console.WriteLine("Expected dat: {0}, Received key: {1}", record.Data.Length, prevNextRecord.Data.Length);
                        result = false;
                    }
                }

                prevRecord = record;
            }

            return result;
        }

        #endregion

    }
}