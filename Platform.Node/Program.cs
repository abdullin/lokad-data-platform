using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Platform.Messages;
using Platform.Node.Services.ServerApi;
using Platform.Node.Services.Storage;
using Platform.Node.Services.Timer;


namespace Platform.Node
{
    class Program
    {
        public static readonly ILogger Log = LogManager.GetLoggerFor<Program>();
        static ManualResetEventSlim _exitWait = new ManualResetEventSlim(false);
        static void Main(string[] args)
        {
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

            var storageService = new FileStorageService(options.StoreLocation, mainQueue);
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
                _exitWait.Wait();
            }
        }

        static void ExitAction(int i)
        {
            _exitWait.Set();
            Environment.Exit(i);
        }
    }


    public enum NodeState
    {
        Initializing,
        Master,
        ShuttingDown,
        ShutDown
    }


    public sealed class NodeController : IHandle<Message>
    {
        IPublisher _outputBus;
        QueuedHandler _mainQueue;
        public static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);


        private static readonly ILogger Log = LogManager.GetLoggerFor<NodeController>();

        readonly FiniteStateMachine<NodeState> _finiteStateMachine;

        NodeState _state = NodeState.Initializing;

        public NodeController(IPublisher outputBus)
        {
            _outputBus = outputBus;

            _finiteStateMachine = CreateFsm();
        }


        FiniteStateMachine<NodeState> CreateFsm()
        {
            return new FsmBuilder<NodeState>()
                .InAllStates()
                    .When<SystemMessage.Init>().Do(Handle)
                    .When<SystemMessage.Start>().Do(Handle)
                    .When<SystemMessage.StorageWriterInitializationDone>().Do(Handle)
                    .When<SystemMessage.BecameWorking>().Do(Handle)

                //    .When<SystemMessage.BecomeWorking>().Do(Handle)
                //    .When<SystemMessage.BecomeShutdown>().Do(Handle)
                .InState(NodeState.Master)
                    .When<ClientMessage.WriteMessage>().Do(m => _outputBus.Publish(m))
                    .When<ClientMessage.RequestShutdown>().Do(Handle)
                    .When<SystemMessage.StartShutdown>().Do(m => Application.Exit(ExitCode.Success, "Shutdown"))
                .InState(NodeState.Initializing)
                    .When<ClientMessage.WriteMessage>().Ignore()

                //    .When<ClientMessage.ReadMessage>().Ignore()
                //.InState(NodeState.ShuttingDown)
                //    .When<SystemMessage.ShutdownTimeout>().Do(Handle)
                //    .InState(NodeState.Master)
                .WhenOther()
                    .Do(m => _outputBus.Publish(m))
                .Build(() => (int)_state);
        }

        public void SetMainQueue(QueuedHandler mainQueue)
        {
            _mainQueue = mainQueue;
        }

        void Handle(ClientMessage.RequestShutdown m)
        {
            _mainQueue.Enqueue(new SystemMessage.StartShutdown());
        }

        void IHandle<Message>.Handle(Message message)
        {
            _finiteStateMachine.Handle(message);
        }

        void Handle(SystemMessage.Init e)
        {
            Log.Info("Initializing");
            _outputBus.Publish(e);

        }

        bool _writerStarted;

        void CheckInitialization()
        {
            if (_writerStarted)
            {
                _mainQueue.Enqueue(new SystemMessage.Start());
            }
        }
        void Handle(SystemMessage.StorageWriterInitializationDone e)
        {
            _writerStarted = true;
            Log.Info("Storage ready");
            _outputBus.Publish(e);
            CheckInitialization();
        }
        
        void Handle(SystemMessage.Start e)
        {
            Log.Info("Starting");
            _outputBus.Publish(e);
            _mainQueue.Enqueue(new SystemMessage.BecameWorking());
        }
        void Handle(SystemMessage.BecameWorking e)
        {
            Log.Info("We are the master");
            _state = NodeState.Master;
            _outputBus.Publish(e);
        }
    }
    }
