using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Platform.Storage;

namespace SmartApp.Sample1.Continuous
{
    class Program
    {
        static void Main(string[] args)
        {
            const int seconds = 1;
            long nextOffcet = 0;
            //Task.Factory.StartNew(() =>
            //                          {
            //                              while (true)
            //                              {
            //                                  Thread.Sleep(seconds * 1000);
            //                                  IAppendOnlyStreamReader reader = new FileAppendOnlyStoreReader(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\Platform.Node\bin\Debug\store"));

            //                                  var records = reader.ReadAll(nextOffcet);
            //                                  if (records.Any())
            //                                  {
            //                                      nextOffcet = records.Last().NextOffset;
            //                                      Console.WriteLine("[{0}] Next offset: {1}", DateTime.Now, nextOffcet);
            //                                  }
            //                              }
            //                          });

            while (true)
            {
                Thread.Sleep(seconds * 1000);
                IAppendOnlyStreamReader reader = new FileAppendOnlyStoreReader(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\Platform.Node\bin\Debug\store"));

                var records = reader.ReadAll(nextOffcet);
                if (records.Any())
                {
                    nextOffcet = records.Last().NextOffset;
                    Console.WriteLine("[{0}] Next offset: {1}", DateTime.Now, nextOffcet);
                }
            }

            Console.ReadKey();

        }
    }
}
