using System;
using System.Threading;
using Platform.TestClient.Commands;
using ServiceStack.ServiceClient.Web;

namespace Platform.TestClient
{
    public class Client
    {
        private static readonly ILogger Log = LogManager.GetLoggerFor<Client>();
        public ClientOptions Options;
        private readonly CommandProcessor _commands = new CommandProcessor(Log);
        public readonly JsonServiceClient JsonClinet = new JsonServiceClient();

        public Client(ClientOptions clientOptions)
        {
            Options = clientOptions;
            JsonClinet = new JsonServiceClient(string.Format("http://{0}:{1}", clientOptions.Ip, clientOptions.HttpPort));

            RegisterCommand();
        }

        private void RegisterCommand()
        {
            _commands.Register(new ExitProcessor());
            _commands.Register(new WriteEventsFloodProcessor());
            _commands.Register(new UsageProcessor(_commands));
        }

        public void Run()
        {
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