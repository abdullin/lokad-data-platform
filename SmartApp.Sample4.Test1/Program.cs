using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartApp.Sample4.Test1
{
    class Program
    {
        static void Main(string[] args)
        {
            string line;
            while ((line = Console.ReadLine())!=null)
            {
                Console.WriteLine(line);
            }
            ;
            //var buffer=new char[10];
            //using (var s = Console.OpenStandardInput())
            //using (var sr = new StreamReader(s))
            //{
            //    sr.Read(buffer, 0, 10);
            //    Console.WriteLine(sr.ReadLine()); 
            //}
            //Console.Write("end test1");

            //Thread.Sleep(10*1000);
        }
    }
}
