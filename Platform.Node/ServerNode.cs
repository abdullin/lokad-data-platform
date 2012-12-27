#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License
// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence
#endregion

using System;
using System.Linq;
using System.Net;
using System.Threading;
using Platform.Messages;
using Platform.Node.Services.ServerApi;
using Platform.Node.Services.Storage;
using Platform.Node.Services.Timer;
using Topshelf;

namespace Platform.Node
{
    public class ServerNode : ServiceControl
    {
        public static readonly ILogger Log = LogManager.GetLoggerFor<Program>();
        static readonly ManualResetEventSlim ExitWait = new ManualResetEventSlim(false);

        public void Start()
        {

        }

        void ExitAction(int i)
        {
            ExitWait.Set();
            Environment.Exit(i);
        }

        public bool Start(HostControl hostControl)
        {
            // This is extremely important to enable high throughput 
            // of individual messages
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = 48;

            var options = new NodeOptions();

            if (!CommandLine.CommandLineParser.Default.ParseArguments(new string[0], options))
            {
                return false;
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
            AzureStoreConfiguration azureConfig;
            if (AzureStoreConfiguration.TryParse(options.StoreLocation, out azureConfig))
            {
                var storageService = new AzureStorageService(azureConfig, mainQueue);
                bus.Subscribe<ClientMessage.AppendEvents>(storageService);
                bus.Subscribe<SystemMessage.Init>(storageService);
                bus.Subscribe<ClientMessage.ImportEvents>(storageService);
                bus.Subscribe<ClientMessage.RequestStoreReset>(storageService);
            }
            else
            {
                var storageService = new FileStorageService(options.StoreLocation, mainQueue);
                bus.Subscribe<ClientMessage.AppendEvents>(storageService);
                bus.Subscribe<SystemMessage.Init>(storageService);
                bus.Subscribe<ClientMessage.ImportEvents>(storageService);
                bus.Subscribe<ClientMessage.RequestStoreReset>(storageService);
            }


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

            if (!interactiveMode)
            {
                ThreadPool.QueueUserWorkItem(x =>
                {
                    Thread.Sleep(1000);
                    hostControl.Stop();
                });
            }

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            ExitWait.Set();
            return true;
        }
    }
}