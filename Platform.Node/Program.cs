using System;
using System.Linq;
using System.Net;
using System.Threading;
using Platform.Messages;
using Platform.Node.Services.ServerApi;
using Platform.Node.Services.Storage;
using Platform.Node.Services.Timer;

namespace Platform.Node
{
    class Program
    {
        public static readonly ILogger Log = LogManager.GetLoggerFor<Program>();
        static readonly ManualResetEventSlim ExitWait = new ManualResetEventSlim(false);
        static void Main(string[] args)
        {

            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = 48;

            var options = new NodeOptions();
            if (!CommandLine.CommandLineParser.Default.ParseArguments(args, options))
            {
                return;
            }

            var list = string.Join(Environment.NewLine,
                options.GetPairs().Select(p => string.Format("{0} : {1}", p.Key, p.Value)));

            Log.Info(list);

            var bus = new InMemoryBus("OutputBus");
            var controller = new NodeController(bus);
            var mainQueue = new QueuedHandler(controller, "Main Queue");
            controller.SetMainQueue(mainQueue);
            Application.Start(ExitAction);
            
            var port = options.HttpPort;

            var http = new PlatformServerApiService(mainQueue, string.Format("http://*:{0}/", port));

            bus.Subscribe<SystemMessage.Init>(http);
            bus.Subscribe<SystemMessage.StartShutdown>(http);


            var timer = new TimerService(new ThreadBasedScheduler(new RealTimeProvider()));
            bus.Subscribe<TimerMessage.Schedule>(timer);

            // switch, based on configuration

            IStorageService storageService;
            AzureStoreConfiguration azureConfig;
            if (AzureStoreConfiguration.TryParse(options.StoreLocation, out azureConfig))
            {
                storageService = new AzureStorageService(azureConfig, mainQueue);
            }
            else
            {
                storageService = new FileStorageService(options.StoreLocation, mainQueue);
            }

            bus.Subscribe<ClientMessage.AppendEvents>(storageService);
            bus.Subscribe<SystemMessage.Init>(storageService);
            bus.Subscribe<ClientMessage.ImportEvents>(storageService);

            mainQueue.Start();

            mainQueue.Enqueue(new SystemMessage.Init());
            
            if (options.KillSwitch > 0)
            {
                var seconds = TimeSpan.FromSeconds(options.KillSwitch);
                mainQueue.Enqueue(
                    TimerMessage.Schedule.Create(seconds, 
                    new PublishEnvelope(mainQueue), 
                    new ClientMessage.RequestShutdown()));
            }
            var interactiveMode = options.KillSwitch <= 0;

            if (interactiveMode)
            {
                Console.Title = string.Format("Test server : {0} : {1}", options.HttpPort, options.StoreLocation);
                Console.WriteLine("Starting everything. Press enter to initiate shutdown");
                Console.ReadLine();
                mainQueue.Enqueue(new ClientMessage.RequestShutdown());
                Console.ReadLine();
            }
            else
            {
                ExitWait.Wait();
            }
        }

        static void ExitAction(int i)
        {
            ExitWait.Set();
            Environment.Exit(i);
        }
    }

}
