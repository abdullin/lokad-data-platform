using System;
using System.IO;
using System.Linq;
using System.Threading;
using Platform.StreamClients;

namespace SmartApp.Sample1.Continuous
{
    class Program
    {
        static void Main(string[] args)
        {
            const int seconds = 1;
            var nextOffset = LoadData();
            ShowData(nextOffset, true);
            var path = @"C:\LokadData\dp-store";
            IInternalStreamClient reader = new FileStreamClient(path);
            while (true)
            {
                var last = reader.ReadAll(nextOffset).LastOrDefault();

                if (!last.IsEmpty)
                {
                    nextOffset = last.Next;
                    ShowData(nextOffset, false);
                    SaveData(nextOffset);
                }
                Thread.Sleep(seconds * 1000);
            }
        }

        private static void ShowData(StorageOffset data, bool dumpData)
        {
            Console.WriteLine("[{2}] Next offset({1}): {0}", data, dumpData ? "from storage" : "real data", DateTime.Now);
        }

        static StorageOffset LoadData()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "sample1.dat");

            if (!File.Exists(path))
                return StorageOffset.Zero;

            long nextOffset = 0;
            long.TryParse(File.ReadAllText(path), out nextOffset);
            return new StorageOffset(nextOffset);
        }

        static void SaveData(StorageOffset nextOffcet)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "sample1.dat");
            using (var sw = new StreamWriter(path, false))
            {
                sw.Write(nextOffcet.OffsetInBytes);
            }
        }
    }
}
