using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Platform.TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Application.Start(Environment.Exit);

            var clientOptions = new ClientOptions();
            if (!CommandLine.CommandLineParser.Default.ParseArguments(args, clientOptions))
            {
                return;
            }

            if (clientOptions.StoreLocation == "env")
            {
                var over = Environment.GetEnvironmentVariable("DATAPLATFORM_STOREDIR");
                if (string.IsNullOrWhiteSpace(over))
                {
                    over = @"C:\LokadData\dp-store";
                }
                clientOptions.StoreLocation = over;
            }

            
            try
            {
                var client = new Client(clientOptions);
                client.Run();
            }
            catch (Exception exception)
            {
                Console.WriteLine("ERROR:");
                Console.Write(exception.Message);
                Console.WriteLine();
            }
        }
    }
}
