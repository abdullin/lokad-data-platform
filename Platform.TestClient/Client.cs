using System;
using System.Threading;
using Platform.TestClient.Commands;
using ServiceStack.ServiceClient.Web;

namespace Platform.TestClient
{
    public class Client
    {
        public ClientOptions Options;
        private readonly CommandProcessor _commands = new CommandProcessor();
        public readonly JsonServiceClient JsonClient = new JsonServiceClient();

        public Client(ClientOptions clientOptions)
        {
            Options = clientOptions;
            JsonClient = new JsonServiceClient(string.Format("http://{0}:{1}", clientOptions.Ip, clientOptions.HttpPort));

            RegisterCommand();
        }

        private void RegisterCommand()
        {
            _commands.Register(new ExitProcessor());
            _commands.Register(new WriteEventsFloodProcessor());
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

                    if(!Execute(args))
                        Console.WriteLine("Fail");
                }
                catch (Exception exception)
                {
                    Console.WriteLine("ERROR:");
                    Console.Write(exception.Message);
                    Console.WriteLine();
                }
                Console.Write(">>> ");
            }
        }

        private static string[] ParseCommandLine(string line)
        {
            return line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private bool Execute(string[] args)
        {
            var context = new CommandProcessorContext(this,new ManualResetEvent(true));

            return  _commands.TryProcess(context, args);
        }
    }
}