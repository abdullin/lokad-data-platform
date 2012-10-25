using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using Platform.StreamClients;

namespace SmartApp.Sample1.Continuous
{
    class Program
    {
        public static string StorePath;

        static void Main()
        {
            StorePath = ConfigurationManager.AppSettings["StorePath"];
            const int seconds = 1;
            var nextOffset = LoadData();
            ShowData(nextOffset, true);
            IInternalStreamClient reader = new FileStreamClient(StorePath);

            var views = Path.Combine(StorePath, "dp-views");
            if (!Directory.Exists(views))
                Directory.CreateDirectory(views);

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
            var path = Path.Combine(StorePath, "dp-views", "sample1.dat");

            if (!File.Exists(path))
                return StorageOffset.Zero;

            long nextOffset;
            long.TryParse(File.ReadAllText(path), out nextOffset);
            return new StorageOffset(nextOffset);
        }

        static void SaveData(StorageOffset nextOffcet)
        {
            var path = Path.Combine(StorePath,"dp-views", "sample1.dat");
            using (var sw = new StreamWriter(path, false))
            {
                sw.Write(nextOffcet.OffsetInBytes);
            }
        }
    }
}
