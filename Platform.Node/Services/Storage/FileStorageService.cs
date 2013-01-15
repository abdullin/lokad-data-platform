using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Platform.Node.Messages;
using Platform.StreamStorage.File;

namespace Platform.Node.Services.Storage
{
    /// <summary>
    /// Wires file-based append-only storage to server messaging infrastructure
    /// </summary>
    public sealed class FileStorageService :
        IHandle<ClientMessage.AppendEvents>,
        IHandle<SystemMessage.Init>,
        IHandle<ClientMessage.ImportEvents>,
        IHandle<ClientMessage.RequestStoreReset>
    {
        readonly static ILogger Log = LogManager.GetLoggerFor<FileStorageService>();
        readonly IPublisher _publisher;

        FileEventStoreManager _managerForServer;
        readonly string _location;
        
        public FileStorageService(string location, IPublisher publisher)
        {
            _location = location;
            _publisher = publisher;
        }

        public void Handle(ClientMessage.AppendEvents message)
        {
            _managerForServer.AppendEventsToStore(message.StoreId, message.StreamId, new[] { message.EventData });

            //Log.Info("Storage service got request");
            message.Envelope(new ClientMessage.AppendEventsCompleted());
        }

        static IEnumerable<byte[]> EnumerateStaging(string location)
        {
            using (var fs = FileMessageSet.OpenForReading(location))
            {
                foreach (var msg in fs.ReadAll(0,int.MaxValue))
                {
                    yield return msg.EventData;
                }
            }
        }

        public void Handle(ClientMessage.ImportEvents msg)
        {
            Log.Info("Got import request: '{0}'", msg.StagingLocation);
            var watch = Stopwatch.StartNew();
            var count = 0;
            var size = 0;


            var lazy = EnumerateStaging(msg.StagingLocation);

            _managerForServer.AppendEventsToStore(msg.StoreId,msg.StreamId, lazy.Select(bytes =>
                {
                    count += 1;
                    size += bytes.Length;
                    return bytes;
                }));
            var totalSeconds = watch.Elapsed.TotalSeconds;
            var speed = size / totalSeconds;
            Log.Info("Import {0} in {1}sec: {2} m/s or {3}", count, Math.Round(totalSeconds, 4), Math.Round(count / totalSeconds), FormatEvil.SpeedInBytes(speed));
            msg.Envelope(new ClientMessage.ImportEventsCompleted());

            ThreadPool.QueueUserWorkItem(state => CleanupFile(msg));
        }

        static void CleanupFile(ClientMessage.ImportEvents msg)
        {
            try
            {
                File.Delete(msg.StagingLocation);
            }
            catch {}
        }

    

        public void Handle(SystemMessage.Init message)
        {
            Log.Info("Storage starting");
            try
            {
                _managerForServer = new FileEventStoreManager(_location);
                _publisher.Publish(new SystemMessage.StorageWriterInitializationDone());
            }
            catch (Exception ex)
            {
                Log.FatalException(ex, "Failed to initialize store: " + ex.Message);
                Application.Exit(ExitCode.Error, "Failed to initialize store: " + ex.Message);
            }
        }

        public void Handle(ClientMessage.RequestStoreReset message)
        {
            _managerForServer.ResetAllStores();
            Log.Info("Storage cleared");
            message.Envelope(new ClientMessage.StoreResetCompleted());
        }
    }
}