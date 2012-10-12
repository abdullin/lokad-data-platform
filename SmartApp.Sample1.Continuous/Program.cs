using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Platform.Storage;

namespace SmartApp.Sample1.Continuous
{
    class Program
    {
        static void Main(string[] args)
        {
            const int seconds = 1;
            long nextOffcet = LoadData();
            ShowData(nextOffcet, true);
            while (true)
            {
                Thread.Sleep(seconds * 1000);
                IAppendOnlyStreamReader reader = new FileAppendOnlyStoreReader(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\Platform.Node\bin\Debug\store"));

                var records = reader.ReadAll(nextOffcet);
                if (records.Any())
                {
                    nextOffcet = records.Last().NextOffset;
                    ShowData(nextOffcet, false);
                    SaveData(nextOffcet);
                }
            }
        }

        private static void ShowData(long data, bool dumpData)
        {
            Console.WriteLine("[{2}] Next offset({1}): {0}", data, dumpData ? "from storage" : "real data", DateTime.Now);
        }

        static long LoadData()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "sample1.dat");

            if (!File.Exists(path))
                return 0;

            long nextOffset = 0;
            long.TryParse(File.ReadAllText(path), out nextOffset);
            return nextOffset;
        }

        static void SaveData(long nextOffcet)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "sample1.dat");
            using (var sw = new StreamWriter(path, false))
            {
                sw.Write(nextOffcet);
            }
        }
    }
}
