using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cqrs.TapeStorage;

namespace Platform.Node
{
    class Program
    {
        private static readonly ManualResetEventSlim _exitEvent = new ManualResetEventSlim(false);
        static void Main(string[] args)
        {
            return;
            var bus = new InMemoryBus("OutputBus");
            var controller = new NodeController(bus);
            var mainQueue = new QueuedHandler(controller, "Main Queue");
            controller.SetMainQueue(mainQueue);
            Application.Start(Environment.Exit);


            var http = new PlatformServerApiService(mainQueue, "http://*:8080/");
            bus.AddHandler<SystemMessage.Init>(http);
            bus.AddHandler<SystemMessage.Shutdown>(http);


            // switch, based on configuration

            var storageService = new StorageService(CreateFileStore, mainQueue);
            bus.AddHandler<ClientMessage.AppendEvents>(storageService);
            bus.AddHandler<SystemMessage.Init>(storageService);

            Console.WriteLine("Starting everything. Press enter to initiate shutdown");

            mainQueue.Start();

            mainQueue.Enqueue(new SystemMessage.Init());

            if (args.Length==0)
            {
                Console.ReadLine();
                mainQueue.Enqueue(new SystemMessage.Shutdown());
                Console.ReadLine();
            }
            else
            {
                Task.Factory.StartNew(() =>
                                          {
                                              Thread.Sleep(int.Parse(args[0])*1000);
                                              Application.Exit(ExitCode.Success,"");
                                          });
                _exitEvent.Wait();
            }
            
        }

        static IAppendOnlyStore CreateFileStore()
        {
            var store =
                new FileAppendOnlyStore(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "store")));
            store.Initialize();
            return store;
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
