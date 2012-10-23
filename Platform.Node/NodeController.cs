using System;
using Platform.Messages;
using Platform.Node.Services.Timer;

namespace Platform.Node
{
    public enum NodeState
    {
        Initializing,
        Master,
        ShuttingDown,
        ShutDown
    }

    public sealed class NodeController : IHandle<Message>
    {


        readonly IPublisher _outputBus;
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
            Log.Info("Shutting down in a few seconds...");
            _outputBus.Publish(TimerMessage.Schedule.Create(TimeSpan.FromSeconds(2), new PublishEnvelope(_mainQueue), new SystemMessage.StartShutdown()));
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