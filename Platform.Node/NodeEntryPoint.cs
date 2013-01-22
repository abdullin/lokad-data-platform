#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License
// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence
#endregion

using System;
using System.Linq;
using System.Net;
using System.Threading;
using Platform.Node.Messages;
using Platform.Node.Services.ServerApi;
using Platform.Node.Services.Storage;
using Platform.Node.Services.Timer;

namespace Platform.Node
{
    /// <summary>
    /// Serves as an execution entry point for the data platform server
    /// </summary>
    public class NodeEntryPoint
    {
        public static readonly ILogger Log = LogManager.GetLoggerFor<Program>();
        readonly ManualResetEventSlim _exitWait;

        readonly QueuedHandler _handler;
        

        public NodeEntryPoint(QueuedHandler handler, ManualResetEventSlim exitWait)
        {
            _handler = handler;
            _exitWait = exitWait;
        }

        public void WaitForServiceToExit()
        {
            _exitWait.Wait();
        }

        public bool WaitForServiceToExit(int seconds)
        {
            return _exitWait.Wait(seconds);
        }

        public void RequestServiceStop()
        {
            _handler.Enqueue(new ClientMessage.RequestShutdown());
        }

        public void RequestServiceStopIn(int timeout)
        {
            var seconds = TimeSpan.FromSeconds(timeout);
            _handler.Enqueue(
                TimerMessage.Schedule.Create(seconds,
                    new PublishEnvelope(_handler),
                    new ClientMessage.RequestShutdown()));
        }

        static NodeEntryPoint()
        {
            // This is extremely important to enable high throughput 
            // of individual messages
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = 48;
        }


        public static NodeEntryPoint StartWithOptions(NodeOptions options, Action<int> termination)
        {
            var slim = new ManualResetEventSlim(false);
            var list = String.Join(Environment.NewLine,
                options.GetPairs().Select(p => String.Format("{0} : {1}", p.Key, p.Value)));

            Log.Info(list);

            var bus = new InMemoryBus("OutputBus");
            var controller = new NodeController(bus);
            var mainQueue = new QueuedHandler(controller, "Main Queue");
            controller.SetMainQueue(mainQueue);
            Application.Start(i =>
                {
                    slim.Set();
                    termination(i);
                });

            var http = new PlatformServerApiService(mainQueue, String.Format("http://{0}:{1}/", options.LocalHttpIp, options.HttpPort));

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
            return new NodeEntryPoint(mainQueue,slim);
        }
    }
}