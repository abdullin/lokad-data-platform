using System;
using System.Configuration;
using Platform.CommandLine;

namespace Platform.Node
{
    /// <summary>
    /// You can run DataPlatform server as a console application. All needed
    /// wiring and management is provided in this class.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {

            var options = new NodeOptions();
            var config = ConfigurationManager.AppSettings["params"] ?? "";

            if (!CommandLineParser.Default.ParseArguments(config.Split(' '), options))
            {
                Console.WriteLine("Can not parse parameters from configuration file.");
                return;
            }

            var cliOptions = new NodeOptions();

            var lineAsString = string.Join(" ", args);

            if (lineAsString == "/?" || lineAsString == "--help")
            {
                Console.WriteLine("Usage");
                Console.WriteLine(cliOptions.GetUsage());
                return;
            }

            if (!CommandLineParser.Default.ParseArguments(args, cliOptions))
            {
                Console.WriteLine("Can not parse parameters from command line.");
                return;
            }

            // Override app.config options with values from command line
            if (!cliOptions.StoreLocation.Equals(NodeOptions.StoreLocationDefault))
                options.StoreLocation = cliOptions.StoreLocation;

            if (cliOptions.HttpPort != NodeOptions.HttpPortDefault)
                options.HttpPort = cliOptions.HttpPort;

            if (cliOptions.KillSwitch != NodeOptions.KillSwitchDefault)
                options.KillSwitch = cliOptions.KillSwitch;

            var node = NodeEntryPoint.StartWithOptions(options, i => Environment.Exit(i));

            if (options.KillSwitch > 0)
            {
                node.RequestServiceStopIn(options.KillSwitch);
            }
            var interactiveMode = options.KillSwitch <= 0;

            if (interactiveMode)
            {
                Console.Title = String.Format("Test server : {0} : {1}", options.HttpPort, options.StoreLocation);
                Console.WriteLine("Starting everything. Press enter to initiate shutdown");
                Console.ReadLine();
                node.RequestServiceStop();
                Console.ReadLine();
            }
            else
            {
                node.WaitForServiceToExit();
            }
        }
    }
}
