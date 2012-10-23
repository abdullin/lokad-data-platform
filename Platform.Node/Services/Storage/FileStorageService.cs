using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Platform.Messages;

namespace Platform.Node.Services.Storage
{
    public sealed class FileStorageService : IStorageService
    {
        readonly static ILogger Log = LogManager.GetLoggerFor<FileStorageService>();
        readonly IPublisher _publisher;

        FileAppendOnlyStore _store;

        readonly string _location;
        
        public FileStorageService(string location, IPublisher publisher)
        {
            _location = location;
            _publisher = publisher;
        }

        public void Handle(ClientMessage.AppendEvents message)
        {
            _store.Append(message.EventStream, new[] { message.Data });

            //Log.Info("Storage service got request");
            message.Envelope(new ClientMessage.AppendEventsCompleted());
        }

        static IEnumerable<byte[]> EnumerateStaging(string location)
        {
            using (var import = File.OpenRead(location))
            using (var bit = new BinaryReader(import))
            {
                var length = import.Length;
                while (import.Position < length)
                {
                    var len = bit.ReadInt32();
                    var data = bit.ReadBytes(len);
                    yield return data;
                }
            }
        }

        public void Handle(ClientMessage.ImportEvents msg)
        {
            Log.Info("Got import request");
            var watch = Stopwatch.StartNew();
            var count = 0;
            var size = 0;
            _store.Append(msg.EventStream, EnumerateStaging(msg.StagingLocation).Select(bytes =>
                {
                    count += 1;
                    size += bytes.Length;
                    return bytes;
                }));
            var totalSeconds = watch.Elapsed.TotalSeconds;
            var speed = size / totalSeconds;
            Log.Info("Import {0} in {1}sec: {2} m/s or {3}", count, Math.Round(totalSeconds, 4), Math.Round(count / totalSeconds), FormatEvil.SpeedInBytes(speed));
            msg.Envelope(new ClientMessage.ImportEventsCompleted());
        }


        public void Handle(SystemMessage.Init message)
        {
            Log.Info("Storage starting");
            try
            {
                _store = new FileAppendOnlyStore(_location);
                _publisher.Publish(new SystemMessage.StorageWriterInitializationDone());
            }
            catch (Exception ex)
            {
                Application.Exit(ExitCode.Error, "Failed to initialize store: " + ex.Message);
            }
        }

        public void Handle(ClientMessage.RequestStoreReset message)
        {
            _store.Reset();
            Log.Info("Storage cleared");
            message.Envelope(new ClientMessage.StoreResetCompleted());
        }
    }
}