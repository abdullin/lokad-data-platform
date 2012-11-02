using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Platform;
using Platform.Storage;
using Platform.StreamClients;
using Platform.ViewClients;

namespace SmartApp.Sample1.Continuous
{
    class Program
    {
        private static IInternalStreamClient _client;
        static ViewClient _view;
        public static string RawDataPath;
        public static string StorePath;
        public static string StoreConnection;
        static string _userName;
        static void Main()
        {
            RawDataPath = ConfigurationManager.AppSettings["RawDataPath"];
            StorePath = ConfigurationManager.AppSettings["StorePath"];
            StoreConnection = ConfigurationManager.AppSettings["StoreConnection"];

            Console.WriteLine("You name:");
            _userName = Console.ReadLine();
            Console.WriteLine("Chat starting...");


            _client = PlatformClient.GetStreamReaderWriter(StorePath, StoreConnection);
            _view = PlatformClient.GetViewClient(StorePath, "sample1");
            _view.CreateContainer();

            Task.Factory.StartNew(ScanChat,
                TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);

            string messages;
            while ((messages = Console.ReadLine()) != "exit")
            {
                _client.WriteEvent("chat", Encoding.UTF8.GetBytes(string.Format("{0}|{1}", _userName, messages)));
            }
        }

        private static void ScanChat()
        {
            var lastMessage = _view.ReadAsJsonOrNull<Sample1LastReadMessage>("sample1.dat");
            var nextOffset =  lastMessage == null ? new StorageOffset(0) : new StorageOffset(lastMessage.LastOffset);
            while (true)
            {
                StorageOffset last=StorageOffset.Zero;
                bool existMessages = false;
                var messages = _client.ReadAll(nextOffset).Where(x => x.Key == "chat");
                foreach (RetrievedDataRecord message in messages)
                {
                    last = message.Next;
                    existMessages = true;
                    var text = Encoding.UTF8.GetString(message.Data);
                    if (text.StartsWith(_userName))
                        continue;
                    Console.WriteLine(text);
                }

                if(existMessages)
                {
                    _view.WriteAsJson(new Sample1LastReadMessage(last.OffsetInBytes), "sample1.dat");
                    nextOffset = last;
                }

                Thread.Sleep(1000);
            }
        }
    }

    class Sample1LastReadMessage
    {
        public Sample1LastReadMessage(long last)
        {
            LastOffset = last;
        }
        public long LastOffset { get; set; }
    }
}
