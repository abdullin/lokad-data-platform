using System;
using System.Collections.Generic;
using System.Threading;
using Platform;
using Platform.StreamClients;
using Platform.ViewClients;

namespace SmartApp.Sample2.Continuous
{
    // Incremental Projection calculating sizes of event messages
    // available
    // See Readme.md in this project for the description of the sample
    class Program
    {
        const string ViewName = "sample2.dat";

        static void Main(string[] args)
        {
            // configure the system
            var store = @"C:\LokadData\dp-store";
            // scan "default" event container
            var reader = PlatformClient.GetStreamReader(store, containerName:"default");
            var views = PlatformClient.GetViewClient(store, "sample2-views");

            // Load view, in case this console continues previous work
            var data = views.ReadAsJsonOrGetNew<Sample2Data>(ViewName);
            // print it for debug purposes
            PrintDataToConsole(data, true);

            // this process runs incrementally until stopped
            while (true)
            {
                try
                {
                    ProcessNextIncrementOfEventsOrSleep(data, reader, views);
                }
                catch (Exception ex)
                {
                    // print and sleep on error
                    Console.WriteLine(ex);
                    Thread.Sleep(1000);
                }
            }
        }

        static void ProcessNextIncrementOfEventsOrSleep(Sample2Data data, IRawStreamClient reader, ViewClient views)
        {
            var nextOffset = data.NextOffset;
            
            // try to read next 10000 events from the platform,
            // starting from the recorded offset.
            // This is more efficient, than reading one event by one, since it
            // reduces cost of reading/writing data by batching
            const int maxRecordCount = 10000;
            var nextEvents = reader.ReadAllEvents(new StorageOffset(nextOffset), maxRecordCount);
            var emptyData = true;
            // process
            foreach (var dataRecord in nextEvents)
            {
                // update next offset
                data.NextOffset = dataRecord.Next.OffsetInBytes;
                // update distribution
                if (data.Distribution.ContainsKey(dataRecord.EventData.Length))
                    data.Distribution[dataRecord.EventData.Length]++;
                else
                    data.Distribution[dataRecord.EventData.Length] = 1;
                emptyData = false;
            }

            if (emptyData)
            {
                // we didn't have any new data, so sleep
                const int seconds = 1;
                Thread.Sleep(seconds * 1000);
            }
            else
            {
                // we had some events incoming, so save projection
                // at least to update offset record
                PrintDataToConsole(data, false);
                views.WriteAsJson(data, ViewName);
            }
        }

        private static void PrintDataToConsole(Sample2Data data, bool dumpData)
        {
            Console.Clear();
            if (dumpData)
                Console.WriteLine("Data from storage!!!");
            Console.WriteLine("Next offset: {0}", data.NextOffset);
            Console.WriteLine("Distribution:");
            foreach (var pair in data.Distribution)
            {
                Console.WriteLine("[{0}]: {1}", pair.Key, pair.Value);
            }
        }
    }

    public class Sample2Data
    {
        public long NextOffset { get; set; }
        public Dictionary<int, int> Distribution { get; set; }

        public Sample2Data()
        {
            Distribution = new Dictionary<int, int>();
        }
    }
}
