using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Platform.Storage;
using Platform.TestClient.Commands;
using ServiceStack.Common;

namespace Platform.TestClient
{
    public class Client
    {
        private static readonly ILogger Log = LogManager.GetLoggerFor<Client>();
        public ClientOptions Options;
        

        private readonly CommandProcessor _commands = new CommandProcessor(Log);
        
        public IInternalPlatformClient Platform;
        public string ClientHttpBase;

        public Client(ClientOptions clientOptions)
        {
            Options = clientOptions;
            // TODO : pass server options

            
            ClientHttpBase = string.Format("http://{0}:{1}", clientOptions.Ip, clientOptions.HttpPort);

            if (clientOptions.StoreLocation.StartsWith("DefaultEndpointsProtocol=", StringComparison.InvariantCultureIgnoreCase)
                || clientOptions.StoreLocation.StartsWith("UseDevelopmentStorage=true", StringComparison.InvariantCultureIgnoreCase))
            {
                var parts = clientOptions.StoreLocation.Split('|');
                Platform = new AzurePlatformClient(connectionString: parts[0], container: parts[1], serverEndpoint: ClientHttpBase);
            }
            else
                Platform = new FilePlatformClient(clientOptions.StoreLocation, ClientHttpBase);

            

            RegisterCommand();
        }

        private void RegisterCommand()
        {
            _commands.Register(new ExitProcessor());
            _commands.Register(new WriteEventsFloodProcessor());
            _commands.Register(new ImportEventsProcessor());
            _commands.Register(new ImportEventsFloodProcessor());
            _commands.Register(new UsageProcessor(_commands));
            _commands.Register(new WriteProccessor());
            _commands.Register(new EnumerateProcessor());
            _commands.Register(new BasicTestProcessor());

            
            _commands.Register(new ShutdownProcessor());
            _commands.Register(new StartLocalServerProcessor());

            _commands.Register(new ResetStoreProcessor());
            
            _commands.Register(new ReadProcessor());
        }

         

        public void Run()
        {
            if (!string.IsNullOrWhiteSpace(Options.RunFile))
            {
                foreach (var ln in 
                    File.ReadAllLines(Options.RunFile)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Where(l => !l.StartsWith("//")))
                {
                    ExecuteLine(ln);
                }
            }


            if (Options.Command.Any())
            {
                ExecuteLine(string.Join(" ", Options.Command));
                return;
            }
            
            
            Console.Write(">>> ");
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    Console.WriteLine("Empty command");
                }
                else
                {
                    ExecuteLine(line);
                }
                Console.Write(">>> ");
            }
        }

        void ExecuteLine(string line)
        {
            try
            {
                var args = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                Log.Info("Processing command: {0}.", string.Join(" ", args));

                var context = new CommandProcessorContext(this, Log);
                _commands.TryProcess(context, args);
            }
            catch (Exception exception)
            {
                Log.ErrorException(exception, "Error while executing command");
            }
        }
    }
}