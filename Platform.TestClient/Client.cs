using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Platform.Storage;
using Platform.Storage.Azure;
using Platform.TestClient.Commands;
using Platform.TestClient.Commands.Bench;
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
            AzureStoreConfiguration azureConfig;
            if (AzureStoreConfiguration.TryParse(clientOptions.StoreLocation, out azureConfig))
            {
                Platform = new AzurePlatformClient(azureConfig, ClientHttpBase);
            }
            else
            {
                Platform = new FilePlatformClient(clientOptions.StoreLocation, ClientHttpBase);
            }



            RegisterCommand();
        }

        private void RegisterCommand()
        {
            _commands.Register(new ExitProcessor());
            _commands.Register(new WriteEventsFloodProcessor());
            _commands.Register(new WriteBatchProcessor());
            _commands.Register(new WriteBatchFloodProcessor());
            _commands.Register(new UsageProcessor(_commands));
            _commands.Register(new WriteProccessor());
            _commands.Register(new EnumerateProcessor());
            _commands.Register(new BasicTestProcessor());
            _commands.Register(new BasicBenchmarkProcessor());
            
            _commands.Register(new ShutdownProcessor());
            _commands.Register(new StartLocalServerProcessor());

            _commands.Register(new ResetStoreProcessor());
            
            _commands.Register(new ReadProcessor());
        }

         

        public void Run()
        {
            if (Options.Command.Any())
            {
                var @join = string.Join(" ", Options.Command);
                if (!ExecuteLine(@join))
                {
                    Application.Exit(ExitCode.Error, "Error while processing " + @join);
                }
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

       

        bool ExecuteLine(string line)
        {
            try
            {
                var args = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                Log.Info("Processing command: {0}.", string.Join(" ", args));

                var context = new CommandProcessorContext(this, Log);
                return _commands.TryProcess(context, args);
            }
            catch (Exception exception)
            {
                Log.ErrorException(exception, "Error while executing command");
                return false;
            }
        }
    }
}