using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Platform.StreamStorage;
using Platform.StreamStorage.Azure;
using Platform.StreamStorage.File;

namespace Platform.TestClient.Commands
{
    public class EventPointerFloodProcessor : ICommandProcessor
    {
        public string Key { get { return "EPW"; } }
        public string Usage { get { return Key + @" [<count=10000>]
    Executes and measures speed of updating event pointer"; } }


        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {

            // [abdullin]: we don't need checkpoint write flood (multi-threads)
            // since there is only one writer by default
            int repeat = 10000;
            if (args.Length > 0)
                int.TryParse(args[0], out repeat);

            const string checkpointName = "epfl.chk";
            AzureStoreConfiguration configuration;
            var location = context.Client.Options.StoreLocation;
            if (AzureStoreConfiguration.TryParse(location, out configuration))
            {

                var container = CloudStorageAccount.Parse(configuration.ConnectionString)
                                                   .CreateCloudBlobClient()
                                                   .GetContainerReference(configuration.RootBlobContainerName);

                container.CreateIfNotExist();
                var blob = container.GetPageBlobReference(checkpointName);
                blob.Create(512);
                try
                {
                    using (var pointer = new TestAzurePointer(blob))
                    {
                        TestPointer(context, pointer, repeat, "page");
                    }
                    using (var pointer = AzureEventPointer.OpenWriteable(blob))
                    {
                        TestPointer(context, pointer, repeat, "meta");
                    }
                }
                finally
                {
                    blob.DeleteIfExists();
                }

            }
            else
            {
                var fullName = Path.Combine(location, checkpointName);
                
                try
                {
                    using (var openOrCreateForWriting = FileEventPointer.OpenOrCreateForWriting(fullName))
                    {
                        TestPointer(context, openOrCreateForWriting, repeat, "file");
                    }
                }
                finally
                {
                    File.Delete(fullName);
                }
                
            }
            return true;    
        }

        void TestPointer(CommandProcessorContext context, IEventPointer pointer, int repeat, string type)
        {
            var stop = Stopwatch.StartNew();
            pointer.Write(0);

            for (int i = 0; i < repeat; i++)
            {
                pointer.Write(i);
            }

            var writesPerSecond = repeat / stop.Elapsed.TotalSeconds;


            context.Log.Debug("Throughput {1}: {0}", Math.Round(writesPerSecond), type);
            context.Log.Debug("Elapsed sec {1}: {0}", stop.Elapsed.TotalSeconds, type);

            var key = string.Format("{0}-{1}-{2}", Key, type, repeat);

            PerfUtils.LogTeamCityGraphData(key + "-writesPerSec", (int) writesPerSecond);
        }


        /// <summary>
        /// Experimental event pointer, which keeps data in page blob directly.
        /// It might be faster than metadata pointer
        /// </summary>
        sealed class TestAzurePointer : IEventPointer
        {
            readonly CloudPageBlob _blob;
            readonly byte[] _buffer = new byte[512];
            public TestAzurePointer(CloudPageBlob blob)
            {
                _blob = blob;
            }

            public void Dispose()
            {
                
            }

            public long Read()
            {
                throw new NotSupportedException();
            }

            public void Write(long position)
            {
                BitConverter.GetBytes(position).CopyTo(_buffer,0);
                using (var stream = new MemoryStream(_buffer))
                {
                    _blob.WritePages(stream, 0);
                }
            }
        }
    }
}