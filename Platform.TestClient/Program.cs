using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Platform.Node;
using ServiceStack.ServiceClient.Web;

namespace Platform.TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //Debugger.Launch();
            Application.Start(Environment.Exit);

            var clientOptions = new ClientOptions
                                    {
                                        Ip = args.Length > 0 ? args[0] : "127.0.0.1",
                                        HttpPort = args.Length > 1 ? args[1] : "8080",
                                        Timeout = args.Length > 2 ? int.Parse(args[2]) : -1,
                                        Command = args.Length > 3 ? args.Skip(3).ToList() : new List<string>(),
                                    };

            var client = new Client(clientOptions);
            try
            {
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
