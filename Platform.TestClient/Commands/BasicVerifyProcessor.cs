using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Platform.StreamClients;
using Platform.StreamStorage;

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

            bool totalResult = WriteFloodAndBatchTogether(context, timeOut, batchSize, batchThreadCount, floodThreadCount);
            totalResult = totalResult & ReadMessageWithNextOffset(context);
            totalResult = totalResult & WriteReadDifferentTypes(context);
            totalResult = totalResult & ReadAndWriteDataFromView(context);

            return totalResult;
        }


        bool WriteFloodAndBatchTogether(CommandProcessorContext context, int timeOut, int batchSize,
            int batchThreadCount, int floodThreadCount)
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
                            context.Client.EventStores.WriteEventsInLargeBatch(streamId,
                            Enumerable.Range(0, batchSize).Select(
                                x =>
                                {
                                    var bytes = Encoding.UTF8.GetBytes(batchMsg);
                                    return (bytes);
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
                                context.Client.EventStores.WriteEvent(streamId, Encoding.UTF8.GetBytes(floodMsg));
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

            context.Log.Info("Add {0} flood messages", floodCount);
            context.Log.Info("Add {0} batch", batchCount);

            var readErrors = ReadAddMessages(context, streamId, batchMsg, floodMsg, batchCount * batchSize, floodCount).ToArray();

            if (readErrors.Any())
                errors.PushRange(readErrors);

            foreach (var err in errors.ToArray())
                context.Log.Error(err);

            return errors.Count == 0;
        }

        private static List<string> ReadAddMessages(CommandProcessorContext context, string streamId, string batchMsg,
            string floodMsg, int batchCount, int floodCount)
        {
            var errors = new List<string>();
            var records = context.Client.EventStores.ReadAllEvents().Where(x => x.StreamId == streamId);
            foreach (var record in records)
            {
                var msg = Encoding.UTF8.GetString(record.EventData);
                if (msg.Equals(batchMsg))
                    batchCount--;
                else if (msg.Equals(floodMsg))
                    floodCount--;
                else
                    errors.Add("strange message: " + msg);
            }

            if (batchCount != 0)
                errors.Add("Unread " + batchCount + " batch messages");
            if (floodCount != 0)
                errors.Add("Unread " + floodCount + " flood messages");

            return errors;
        }

        private bool ReadMessageWithNextOffset(CommandProcessorContext context)
        {
            var result = true;
            var records = context.Client.EventStores.ReadAllEvents(maxRecordCount: 20).ToArray();

            if (records.Length == 0)
                return true;

            RetrievedEventWithMetaData prevRecord = records[0];

            for (int i = 1; i < records.Length; i++)
            {
                var prevNextRecord = context.Client.EventStores.ReadAllEvents(prevRecord.Next, 1).First();
                var expectedBytes = records[i].EventData.Except(prevNextRecord.EventData).ToList();
                if (records[i].StreamId != prevNextRecord.StreamId | expectedBytes.Count != 0)
                {
                    context.Log.Error("Expected key: {0}, Received key: {1}", records[i].StreamId, prevNextRecord.StreamId);
                    context.Log.Error("Expected dat: {0}, Received key: {1}", records[i].EventData.Length, prevNextRecord.EventData.Length);
                    result = false;
                }
                prevRecord = records[i];
            }

            return result;
        }

        bool WriteReadDifferentTypes(CommandProcessorContext context)
        {
            string streamId = Guid.NewGuid().ToString();

            int intVal = 101;
            long longVal = 102;
            char charVal = 'A';
            string stringVal = "Hello server";
            DateTime dateVal = new DateTime(2012, 10, 25, 1, 2, 3);
            double doubleVal = 103.0;

            context.Client.EventStores.WriteEvent(streamId, BitConverter.GetBytes(intVal));
            context.Client.EventStores.WriteEvent(streamId, BitConverter.GetBytes(longVal));
            context.Client.EventStores.WriteEvent(streamId, BitConverter.GetBytes(charVal));
            context.Client.EventStores.WriteEvent(streamId, Encoding.UTF8.GetBytes(stringVal));
            context.Client.EventStores.WriteEvent(streamId, BitConverter.GetBytes(dateVal.ToBinary()));
            context.Client.EventStores.WriteEvent(streamId, BitConverter.GetBytes(doubleVal));

            var batchBody = new List<byte[]>
                           {
                               (BitConverter.GetBytes(intVal)),
                               (BitConverter.GetBytes(longVal)),
                               (BitConverter.GetBytes(charVal)),
                               (Encoding.UTF8.GetBytes(stringVal)),
                               (BitConverter.GetBytes(dateVal.ToBinary())),
                               (BitConverter.GetBytes(doubleVal))
                           };

            context.Client.EventStores.WriteEventsInLargeBatch(streamId, batchBody);

            var records = context.Client.EventStores.ReadAllEvents().Where(x => x.StreamId == streamId).ToArray();
            bool result = true;

            for (int i = 0; i < 2; i++)
            {
                if (BitConverter.ToInt32(records[i * 6 + 0].EventData, 0) != intVal)
                {
                    context.Log.Error("could not read the INT");
                    result = false;
                }
                if (BitConverter.ToInt64(records[i * 6 + 1].EventData, 0) != longVal)
                {
                    context.Log.Error("could not read the LONG");
                    result = false;
                }
                if (BitConverter.ToChar(records[i * 6 + 2].EventData, 0) != charVal)
                {
                    context.Log.Error("could not read the CHAR");
                    result = false;
                }
                if (Encoding.UTF8.GetString(records[i * 6 + 3].EventData) != stringVal)
                {
                    context.Log.Error("could not read the STRING");
                    result = false;
                }
                if (DateTime.FromBinary(BitConverter.ToInt64(records[i * 6 + 4].EventData, 0)) != dateVal)
                {
                    context.Log.Error("could not read the DATETIME");
                    result = false;
                }
                if (BitConverter.ToDouble(records[i * 6 + 5].EventData, 0) != doubleVal)
                {
                    context.Log.Error("could not read the DOUBLE");
                    result = false;
                }
            }


            return result;
        }

        bool ReadAndWriteDataFromView(CommandProcessorContext context)
        {
            bool result = true;
            var views = context.Client.Views;
            views.CreateContainer();

            string streamId = Guid.NewGuid().ToString();
            var testData = Enumerable.Range(1, 100);

            context.Client.EventStores.WriteEventsInLargeBatch(streamId, testData.Select(x => (BitConverter.GetBytes(x))));

            var data = views.ReadAsJsonOrGetNew<IntDistribution>(IntDistribution.FileName);

            var records = context.Client.EventStores.ReadAllEvents(new StorageOffset(data.NextOffsetInBytes)).Where(x => x.StreamId == streamId);

            foreach (var record in records)
            {
                data.Distribution.Add(BitConverter.ToInt32(record.EventData, 0));
                data.NextOffsetInBytes = record.Next.OffsetInBytes;
            }

            views.WriteAsJson(data, IntDistribution.FileName);

            var writedData = views.ReadAsJsonOrGetNew<IntDistribution>(IntDistribution.FileName);

            if (data.NextOffsetInBytes != writedData.NextOffsetInBytes)
            {
                context.Log.Error("Different Next.OffsetInBytes");
                result = false;
            }
            if (data.Distribution.Count != writedData.Distribution.Count)
            {
                context.Log.Error("Different records count");
                result = false;
                return result;
            }

            for (int i = 0; i < data.Distribution.Count; i++)
            {
                if (data.Distribution[i] != writedData.Distribution[i])
                {
                    context.Log.Error("Different record value");
                    result = false;
                }
            }

            return result;
        }

        private class IntDistribution
        {
            public long NextOffsetInBytes { get; set; }
            public List<int> Distribution { get; private set; }
            public const string FileName = "ViewClientTest.dat";

            public IntDistribution()
            {
                Distribution = new List<int>();
            }
        }

    }


}