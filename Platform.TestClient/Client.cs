using System;
using System.Linq;
using System.Threading;
using Platform.TestClient.Commands;
using ServiceStack.Common;
using ServiceStack.ServiceClient.Web;

namespace Platform.TestClient
{
    public class Client
    {
        private static readonly ILogger Log = LogManager.GetLoggerFor<Client>();
        public ClientOptions Options;
        public readonly JsonServiceClient JsonClient = new JsonServiceClient();

        private readonly CommandProcessor _commands = new CommandProcessor(Log);
        private readonly bool _interactiveMode;

        public Client(ClientOptions clientOptions)
        {
            Options = clientOptions;
            JsonClient = new JsonServiceClient(string.Format("http://{0}:{1}", clientOptions.Ip, clientOptions.HttpPort));

            _interactiveMode = clientOptions.Command.IsEmpty();

            RegisterCommand();
        }

        private void RegisterCommand()
        {
            _commands.Register(new ExitProcessor());
            _commands.Register(new WriteEventsFloodProcessor());
            _commands.Register(new UsageProcessor(_commands));
            _commands.Register(new WriteProccessor());
        }

        public void Run()
        {
            Log.Debug("##teamcity[message text='test' errorDetails='Test details' status='ERROR']");
            if(!_interactiveMode)
            {
                Execute(Options.Command.ToArray());
                return;
            }
            
            Console.Write(">>> ");
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    Console.WriteLine("Empty command");
                    Console.Write(">>> ");
                    continue;
                }

                try
                {
                    var args = ParseCommandLine(line);

                    Execute(args);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("ERROR:");
                    Console.Write(exception.Message);
                    Console.WriteLine();
                }
                //Console.Write(">>> ");
            }
        }

        private static string[] ParseCommandLine(string line)
        {
            return line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private bool Execute(string[] args)
        {
            Log.Info("Processing command: {0}.", string.Join(" ", args));
            var context = new CommandProcessorContext(this, Log, new ManualResetEvent(true));

            return _commands.TryProcess(context, args);
        }
    }
}