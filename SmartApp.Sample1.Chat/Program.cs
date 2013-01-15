using System;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Platform;
using Platform.StreamClients;
using Platform.ViewClients;

namespace SmartApp.Sample1.Chat
{
    // Simple chat application using DataPlatform store for communication
    // and message exchange between multiple clients
    // See Readme.md in this project for the description of the sample
    class Program
    {
        private static IRawEventStoreClient _client;
        static ViewClient _view;
        public static string StorePath;
        public static string StoreConnection;
        static string _userName;
        static string _userMessage;
        static void Main()
        {
            
            StorePath = ConfigurationManager.AppSettings["StorePath"];

            if (string.IsNullOrWhiteSpace(StorePath))
                StorePath = @"C:\LokadData\dp-store";

            StoreConnection = ConfigurationManager.AppSettings["StoreConnection"];
            if (string.IsNullOrWhiteSpace(StoreConnection))
                StoreConnection = "http://localhost:8080";

            // Use "default" container for reading/writing events
            _client = PlatformClient.GetStreamReaderWriter(StorePath, StoreConnection, containerName:"default");
            _view = PlatformClient.GetViewClient(StorePath, "sample1-views");
            _view.CreateContainer();

            Console.WriteLine("You name:");
            _userName = Console.ReadLine();
            Console.WriteLine("Chat starting...");

            _client.WriteEvent("", Encoding.UTF8.GetBytes("|join a new user " + _userName));
            Task.Factory.StartNew(ScanChat,
                TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);

            WriteColorText(_userName + ">", ConsoleColor.Green);

            _userMessage = "";

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey();

                if (keyInfo.KeyChar != '\r')
                    _userMessage += keyInfo.KeyChar;
                else
                {
                    _client.WriteEvent("", Encoding.UTF8.GetBytes(string.Format("{0}|{1}", _userName, _userMessage)));
                    Console.WriteLine();
                    WriteColorText(_userName + ">", ConsoleColor.Green);
                    _userMessage = "";
                }
            }
        }

        private static void ScanChat()
        {
            var lastMessage = _view.ReadAsJsonOrNull<Sample1LastReadMessage>("sample1.dat");
            var nextOffset = lastMessage == null ? new StorageOffset(0) : new StorageOffset(lastMessage.LastOffset);
            while (true)
            {
                var last = StorageOffset.Zero;
                var existMessages = false;
                foreach (var message in _client.ReadAllEvents(nextOffset))
                {
                    last = message.Next;
                    existMessages = true;
                    var text = Encoding.UTF8.GetString(message.EventData);
                    var userName = text.Split('|')[0];
                    var msg = text.Split(new[] { '|' }, 2)[1];
                    if (userName == _userName)
                        continue;

                    ClearCurrentConsoleLine();
                    if (userName != "")
                    {
                        WriteColorText(userName + ">", ConsoleColor.Red);
                        Console.WriteLine(msg);
                    }
                    else
                    {
                        WriteColorText(msg + Environment.NewLine, ConsoleColor.DarkCyan);
                    }

                    WriteColorText(_userName + ">", ConsoleColor.Green);
                    Console.Write(_userMessage);
                }

                if (existMessages)
                {
                    _view.WriteAsJson(new Sample1LastReadMessage(last.OffsetInBytes), "sample1.dat");
                    nextOffset = last;
                }

                Thread.Sleep(1000);
            }
        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }

        private static void WriteColorText(string text, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = oldColor;
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
