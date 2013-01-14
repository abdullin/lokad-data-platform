using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Platform.StreamClients;
using ServiceStack.Text;

namespace SmartApp.Sample2.Continuous
{
    // See Readme.md in this project for the description of the sample
    class Program
    {
        static void Main(string[] args)
        {
            // configure the system
            var client = new FileStreamClient(@"C:\LokadData\dp-store");


            // Load view, in case this console continues previous work
            var data = LoadProjectedView();
            // print it for debug purposes
            PrintDataToConsole(data, true);

            // this process runs incrementally until stopped
            while (true)
            {
                try
                {
                    
                    ProcessNextIncrementOfEventsOrSleep(data, client);
                }
                catch (Exception ex)
                {
                    // print and sleep on error
                    Console.WriteLine(ex);
                    Thread.Sleep(1000);
                }
            }
        }

        static void ProcessNextIncrementOfEventsOrSleep(Sample2Data data, IInternalStreamClient reader)
        {
            var nextOffset = data.NextOffset;

            
            
            // try to read next events from the platform,
            // starting from the specified offset
            var nextEvents = reader.ReadAll(new StorageOffset(nextOffset), int.MaxValue);
            var emptyData = true;

            foreach (var dataRecord in nextEvents)
            {
                // update next offset
                data.NextOffset = dataRecord.Next.OffsetInBytes;
                // update distribution
                if (data.Distribution.ContainsKey(dataRecord.Data.Length))
                    data.Distribution[dataRecord.Data.Length]++;
                else
                    data.Distribution[dataRecord.Data.Length] = 1;
                emptyData = false;
            }

            if (!emptyData)
            {
                // we had some events incoming, so save projection
                // at least to update offset record
                PrintDataToConsole(data, false);
                SaveData(data.ToJson());
            }
            else
            {
                // we didn't have any new data, so sleep
                const int seconds = 1;
                Thread.Sleep(seconds * 1000);
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

        static Sample2Data LoadProjectedView()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "sample2.dat");

            if (!File.Exists(path))
                return new Sample2Data { NextOffset = 0, Distribution = new Dictionary<int, int>() };

            return File.ReadAllText(path).FromJson<Sample2Data>();
        }

        static void SaveData(string jsonData)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "sample2.dat");
            using (var sw = new StreamWriter(path, false))
            {
                sw.Write(jsonData);
            }
        }
    }

    public class Sample2Data
    {
        public long NextOffset { get; set; }
        public Dictionary<int, int> Distribution { get; set; }
    }
}
