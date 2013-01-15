using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAzure.StorageClient;
using Platform.Node.Messages;
using Platform.StreamStorage.Azure;

namespace Platform.Node.Services.Storage
{
    /// <summary>
    /// Wires Azure append-only storage to server messaging infrastructure
    /// </summary>
    public sealed class AzureStorageService :
        IHandle<ClientMessage.AppendEvents>,
        IHandle<SystemMessage.Init>,
        IHandle<ClientMessage.ImportEvents>,
        IHandle<ClientMessage.RequestStoreReset>

    {
        readonly static ILogger Log = LogManager.GetLoggerFor<AzureStorageService>();
        readonly IPublisher _publisher;
        readonly AzureStoreConfiguration _config;
        AzureEventStoreManager _manager;

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
                _manager = new AzureEventStoreManager(_config);
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
            _manager.AppendEventsToStore(message.StoreId, message.StreamId, new[] { message.EventData });

            Log.Info("Storage service got request");
            message.Envelope(new ClientMessage.AppendEventsCompleted());
        }

        public void Handle(ClientMessage.ImportEvents msg)
        {
            Log.Info("Got import request for {0} bytes", msg.Size);
            var watch = Stopwatch.StartNew();
            var count = 0;
            var size = 0;
            var blob = _config.GetPageBlob(msg.StagingLocation);
            _manager.AppendEventsToStore(msg.StoreId, msg.StreamId, EnumerateStaging(blob,msg.Size).Select(bytes =>
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
            _manager.ResetAllStores();
            Log.Info("Storage cleared");
            message.Envelope(new ClientMessage.StoreResetCompleted());
        }

        static IEnumerable<byte[]> EnumerateStaging(CloudPageBlob location, long size)
        {
            using (var fs = AzureEventStoreChunk.OpenExistingForReading(location, size))
            {
                foreach (var msg in fs.ReadAll(0,size, int.MaxValue))
                {
                    yield return msg.EventData;
                }
            }
        }
    }
}
