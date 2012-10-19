using System.Threading;
using Platform.Messages;
using Platform.Node.Services.ServerApi;
using Platform.Node.Services.Storage;
using Platform.Node.Services.Timer;

namespace Platform.Node.Worker
{
    public class Host
    {
        readonly PlatformServerApiService _http;
        static readonly ManualResetEventSlim ExitWait = new ManualResetEventSlim(false);
        readonly InMemoryBus _bus;
        readonly QueuedHandler _mainQueue;
        readonly AzureStorageService _storageService;
        readonly TimerService _timer;

        public Host(string storageConnection, string container, string endpoint)
        {
            _http = new PlatformServerApiService(_mainQueue, endpoint);

            _bus = new InMemoryBus("OutputBus");
            var controller = new NodeController(_bus);
            _mainQueue = new QueuedHandler(controller, "Main Queue");
            controller.SetMainQueue(_mainQueue);

            _storageService = new AzureStorageService(storageConnection, container, _mainQueue);
            _timer = new TimerService(new ThreadBasedScheduler(new RealTimeProvider()));
        }

        public void Run()
        {
            Application.Start(i => ExitWait.Set());

            _bus.Subscribe<SystemMessage.Init>(_http);
            _bus.Subscribe<SystemMessage.StartShutdown>(_http);
            _bus.Subscribe(_timer);
            _bus.Subscribe<ClientMessage.AppendEvents>(_storageService);
            _bus.Subscribe<SystemMessage.Init>(_storageService);
            _bus.Subscribe<ClientMessage.ImportEvents>(_storageService);

            _mainQueue.Start();
            _mainQueue.Enqueue(new SystemMessage.Init());

            ExitWait.Wait();
            _mainQueue.Stop();
        }

        public void Cancel()
        {
            _mainQueue.Enqueue(new ClientMessage.RequestShutdown());
        }
    }
}
