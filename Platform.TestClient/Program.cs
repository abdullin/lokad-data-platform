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
            var client = new JsonServiceClient("http://localhost:81");
            ;
            Console.ReadLine();


            long total = 0;

            int count = 0;


           

            var threads = new List<Task>();

            var size = 1000;

            var global = Stopwatch.StartNew();

            for (int t = 0; t < 5; t++)
            {

                var task = Task.Factory.StartNew(() =>
                    {
                        var watch = Stopwatch.StartNew();

                        for (int i = 0; i < size; i++)
                        {
                            client.Post<ClientDto.WriteEvent>("/stream", new ClientDto.WriteEvent()
                                {
                                    Data = Encoding.UTF8.GetBytes("This is some test message to load the server"),
                                    Stream = "name",
                                    ExpectedVersion = -1
                                });
                            //client.Get<ClientDto.WriteEvent>("/stream/name");
                        }
                        Interlocked.Add(ref total, watch.Elapsed.Ticks);
                        Interlocked.Add(ref count, size);

                        //  Console.WriteLine(watch.Elapsed.Ticks);
                    }, TaskCreationOptions.LongRunning| TaskCreationOptions.PreferFairness);
                threads.Add(task);

            }


            Task.WaitAll(threads.ToArray());

            Console.WriteLine("{0} per second", count / global.Elapsed.TotalSeconds);
            Console.ReadLine();
        }
    }
}
