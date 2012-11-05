using System;
using System.IO;
using System.Net;

namespace Platform.TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Application.Start(Environment.Exit);
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = 48;

            var options = new ClientOptions();
            if (!CommandLine.CommandLineParser.Default.ParseArguments(args, options))
            {
                Console.WriteLine(options.GetUsage());
                return;
            }
            
            try
            {
                if (options.Command.Count == 0 && File.Exists("Readme.md"))
                {
                    Console.WriteLine(File.ReadAllText("Readme.md"));
                }

                foreach (var pair in options.GetPairs())
                {
                    Console.WriteLine("  {0,15} : {1}", pair.Key.ToUpperInvariant(), pair.Value);
                }
                Console.WriteLine();

                var client = new Client(options);
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
