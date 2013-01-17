using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Platform.StreamStorage;
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

            using (var pointer = GetEventPointer(context))
            {
                var stop = Stopwatch.StartNew();
                pointer.Write(0);

                for (int i = 0; i < repeat; i++)
                {
                    pointer.Write(i);
                }

                var writesPerSecond = repeat / stop.Elapsed.TotalSeconds;



                context.Log.Debug("Throughput: {0}", Math.Round(writesPerSecond));
                context.Log.Debug("Elapsed sec: {0}", stop.Elapsed.TotalSeconds);
                var key = string.Format("{0}-{1}", Key, repeat);

                PerfUtils.LogTeamCityGraphData(key + "-writesPerSec", (int)writesPerSecond);
            }
            return true;    
        }

         IEventPointer GetEventPointer(CommandProcessorContext context)
        {
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


                //var azurePointer = AzureEventPointer.OpenWriteable(blob);
                var azurePointer = new TestAzurePointer(blob);
                return new TestEventPointer(azurePointer, () => blob.DeleteIfExists());
            }
             var fullName = Path.Combine(location, checkpointName);
             return new TestEventPointer(FileEventPointer.OpenOrCreateForWriting(fullName), () => File.Delete(fullName));
        }
        /// <summary>
        /// Experimental event pointer, which keeps data in page blob directly
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


        sealed class TestEventPointer : IEventPointer
        {
            readonly IEventPointer _pointer;
            readonly Action _onDisposal;

            public TestEventPointer(IEventPointer pointer, Action onDisposal)
            {
                _pointer = pointer;
                _onDisposal = onDisposal;
            }

            public void Dispose()
            {
                _pointer.Dispose();
                _onDisposal();
            }

            public long Read()
            {
                return _pointer.Read();
            }

            public void Write(long position)
            {
                _pointer.Write(position);
            }
        }
    }
}