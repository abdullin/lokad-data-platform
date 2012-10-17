#region (c) 2012 Lokad Data Platform - New BSD License 

// Copyright (c) Lokad 2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Platform.Storage;

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
            get { return "BasicTest [brachcount batchsize threadcount floodsize]"; }
        }

        const string singleThreadMessageTemplate = "basic-test-one-thread-message-{0}-{{0}}";

        public bool Execute(CommandProcessorContext context, string[] args)
        {
            int batchCount = 10;
            int batchSize = 10000;
            int threadCount = 10;
            int floodSize = 1000;

            if (args.Length > 0)
                int.TryParse(args[0], out batchCount);
            if (args.Length > 1)
                int.TryParse(args[1], out batchSize);
            if (args.Length > 2)
                int.TryParse(args[2], out threadCount);
            if (args.Length > 3)
                int.TryParse(args[3], out floodSize);

            string streamId = "BasicTest-" + Guid.NewGuid();


            ImportBatch(context, streamId, batchCount, batchSize);
            FloodWrite(context, streamId, threadCount, floodSize);

            var records = context.Client.Platform.ReadAll(0).Where(x => x.Key == streamId);

            if (!ValidateRecordsCount(context, records, batchCount, batchSize, threadCount, floodSize))
                return false;

            if (!ValidateBatchMessages(context, records, batchCount, batchSize))
                return false;

            if (!ValidateFloodMessages(context, records, threadCount, floodSize, batchCount, batchSize))
                return false;

            return true;
        }

        static bool ValidateFloodMessages(CommandProcessorContext context, IEnumerable<RetrievedDataRecord> records,
            int threadCount, int floodSize,
            int batchCount, int batchSize)
        {
            foreach (var record in records.Take(threadCount * floodSize).Skip(batchCount * batchSize))
            {
                string receivedMessage = Encoding.UTF8.GetString(record.Data);
                var recordArguments =
                    receivedMessage.Replace("basic-test-more-thread-message-", "").Split('-').Select(x => int.Parse(x)).
                        ToArray();

                if (recordArguments.Length != 2 || recordArguments[0] < 0 || recordArguments[0] >= threadCount ||
                    recordArguments[1] < 0 || recordArguments[1] >= floodSize)
                {
                    context.Log.Error(string.Format("Received: {0}", receivedMessage));
                    return false;
                }
            }
            return true;
        }

        static bool ValidateBatchMessages(CommandProcessorContext context, IEnumerable<RetrievedDataRecord> records,
            int batchCount,
            int batchSize)
        {
            int indexBatchCount = 0;
            int indexBatchSize = 0;

            foreach (var record in records.Take(batchCount * batchSize))
            {
                string expectedMessage = string.Format(singleThreadMessageTemplate, indexBatchCount);
                expectedMessage = string.Format(expectedMessage, indexBatchSize);
                string receivedMessage = Encoding.UTF8.GetString(record.Data);
                if (receivedMessage != expectedMessage)
                {
                    context.Log.Error("Expected: {0}, Received: {1}", expectedMessage, receivedMessage);
                    return false;
                }

                indexBatchSize++;
                if (indexBatchSize == batchSize)
                {
                    indexBatchCount++;
                    indexBatchSize = 0;
                }
            }
            return true;
        }

        static bool ValidateRecordsCount(CommandProcessorContext context, IEnumerable<RetrievedDataRecord> records,
            int batchCount,
            int batchSize, int threadCount, int floodSize)
        {
            var recordsCount = records.Count();
            if (recordsCount != batchCount * batchSize + threadCount * floodSize)
            {
                context.Log.Error("Expected: {0} messages, Received: {1} messages",
                    batchCount * batchSize + threadCount * floodSize,
                    recordsCount);
                return false;
            }
            return true;
        }

        static void FloodWrite(CommandProcessorContext context, string streamId, int threadCount, int floodSize)
        {
            var threads = new List<Task>();
            for (int t = 0; t < threadCount; t++)
            {
                int t1 = t;
                var task = Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < floodSize; i++)
                        {
                            context.Client.Platform.WriteEvent(streamId,
                                Encoding.UTF8.GetBytes(
                                    string.Format(
                                        "basic-test-more-thread-message-{0}-{1}",
                                        t1, i)));
                        }
                    }, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
                threads.Add(task);
            }
            Task.WaitAll(threads.ToArray());
        }

        static void ImportBatch(CommandProcessorContext context, string streamId, int batchCount, int batchSize)
        {
            for (int i = 0; i < batchCount; i++)
            {
                string message = string.Format(singleThreadMessageTemplate, i);
                context.Client.Platform.ImportBatch(streamId,
                    Enumerable.Range(0, batchSize).Select(
                        x =>
                            new RecordForStaging(Encoding.UTF8.GetBytes(string.Format(message, x)))));
            }
        }
    }
}