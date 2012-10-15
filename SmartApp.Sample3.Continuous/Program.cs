using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Platform;
using Platform.Storage;
using ServiceStack.Text;
using SmartApp.Sample3.Dump;

namespace SmartApp.Sample3.Continuous
{
    class Program
    {
        static void Main(string[] args)
        {
            const int seconds = 1;
            var data = LoadData();
            ShowData(data, true);
            while (true)
            {
                long nextOffcet = data.NextOffset;
                Thread.Sleep(seconds * 1000);
                IPlatformClient reader = new FilePlatformClient(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\Platform.Node\bin\Debug\store"));

                var records = reader.ReadAll(nextOffcet);
                bool emptyData = true;
                foreach (var dataRecord in records)
                {
                    data.NextOffset = dataRecord.NextOffset;

                    if (dataRecord.Data.Length == 0 || dataRecord.Data[0] != 43)
                        continue;

                    var bytes = dataRecord.Data.Skip(1).ToArray();

                    var json = Encoding.UTF8.GetString(bytes);
                    if (!json.StartsWith("{"))
                        continue;

                    var post = json.FromJson<Post>();
                    if (post == null)
                        continue;

                    foreach (var tag in post.Tags)
                    {
                        if (data.Distribution.ContainsKey(tag))
                            data.Distribution[tag]++;
                        else
                            data.Distribution[tag] = 1;
                    }


                    emptyData = false;
                }

                if (!emptyData)
                {
                    ShowData(data, false);
                    SaveData(data.ToJson());
                }
            }
        }

        private static void ShowData(Sample3Data data, bool dumpData)
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

        static Sample3Data LoadData()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "sample3-tag-count.dat");

            if (!File.Exists(path))
                return new Sample3Data { NextOffset = 0, Distribution = new Dictionary<string, long>() };

            return File.ReadAllText(path).FromJson<Sample3Data>();
        }

        static void SaveData(string jsonData)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "sample3-tag-count.dat");
            using (var sw = new StreamWriter(path, false))
            {
                sw.Write(jsonData);
            }
        }
    }

    public class Sample3Data
    {
        public long NextOffset { get; set; }

        public Dictionary<string, long> Distribution { get; set; }
    }
}
