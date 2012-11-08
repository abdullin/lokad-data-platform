using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAzure.StorageClient;
using Platform.Messages;
using Platform.Storage.Azure;

namespace Platform.Node.Services.Storage
{
    public sealed class AzureStorageService :
        IHandle<ClientMessage.AppendEvents>,
        IHandle<SystemMessage.Init>,
        IHandle<ClientMessage.ImportEvents>,
        IHandle<ClientMessage.RequestStoreReset>

    {
        readonly static ILogger Log = LogManager.GetLoggerFor<AzureStorageService>();
        readonly IPublisher _publisher;
        readonly AzureStoreConfiguration _config;
        AzureContainerManager _manager;

        public AzureStorageService(AzureStoreConfiguration config, IPublisher publisher)
        {
            _publisher = publisher;
            _config = config;
        }

        public void Handle(SystemMessage.Init message)
        {
            Log.Info("Storage starting");
            try
            {
                _manager = new AzureContainerManager(_config);
                _publisher.Publish(new SystemMessage.StorageWriterInitializationDone());
            }
            catch (Exception ex)
            {
                Log.FatalException(ex, "Failed to initialize store: " + ex.Message);
                Application.Exit(ExitCode.Error, "Failed to initialize store: " + ex.Message);
            }
        }

        public void Handle(ClientMessage.AppendEvents message)
        {
            _manager.Append(message.Container, message.StreamKey, new[] { message.Data });

            Log.Info("Storage service got request");
            message.Envelope(new ClientMessage.AppendEventsCompleted());
        }

        public void Handle(ClientMessage.ImportEvents msg)
        {
            Log.Info("Got import request");
            var watch = Stopwatch.StartNew();
            var count = 0;
            var size = 0;
            var client = StorageExtensions.GetCloudBlobClient(_config.ConnectionString);
            var blob = client.GetBlockBlobReference(msg.StagingLocation);
            _manager.Append(msg.Container, msg.StreamKey, EnumerateStaging(blob).Select(bytes =>
                {
                    count += 1;
                    size += bytes.Length;
                    return bytes;
                }));
            var totalSeconds = watch.Elapsed.TotalSeconds;
            var speed = size / totalSeconds;
            Log.Info("Import {0} in {1}sec: {2} m/s or {3}", count, Math.Round(totalSeconds, 4), Math.Round(count / totalSeconds), FormatEvil.SpeedInBytes(speed));

            msg.Envelope(new ClientMessage.ImportEventsCompleted());

            ThreadPool.QueueUserWorkItem(state => CleanupBlob(blob));
        }

        static void CleanupBlob(CloudBlob blob)
        {
            try
            {
                blob.DeleteIfExists();
            }
            catch (Exception ex) {}
        }

        public void Handle(ClientMessage.RequestStoreReset message)
        {
            _manager.Reset();
            Log.Info("Storage cleared");
            message.Envelope(new ClientMessage.StoreResetCompleted());
        }

        static IEnumerable<byte[]> EnumerateStaging(CloudBlob blob)
        {
            using (var stream = blob.OpenRead())
            using (var reader = new BinaryReader(stream))
            {
                blob.FetchAttributes();
                var length = blob.Properties.Length;
                while (stream.Position < length)
                {
                    var len = reader.ReadInt32();
                    var data = reader.ReadBytes(len);
                    yield return data;
                }
            }
        }
    }
}
