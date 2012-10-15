using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Platform.Messages;

namespace Platform.Node.Services.Storage
{
    public sealed class StorageService : 
        
        IHandle<ClientMessage.AppendEvents>,
        IHandle<SystemMessage.Init>,
        IHandle<ClientMessage.ImportEvents>
    {
        readonly static ILogger Log = LogManager.GetLoggerFor<StorageService>();
        readonly IPublisher _publisher;

        readonly Func<IAppendOnlyStore> _func;

        IAppendOnlyStore _store;
        

        
        public StorageService(Func<IAppendOnlyStore> func, IPublisher publisher)
        {
            _func = func;
            _publisher = publisher;
        }

        public void Handle(ClientMessage.AppendEvents message)
        {
            _store.Append(message.EventStream, new[] { message.Data }, message.ExpectedVersion);
            

            Log.Info("Storage service got request");
            message.Envelope(new ClientMessage.AppendEventsCompleted());
        }

        IEnumerable<byte[]> EnumerateStaging(string location)
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
            _store.Append(msg.EventStream, EnumerateStaging(msg.StagingLocation).Select(bytes =>
                {
                    count += 1;
                    return bytes;
                }), msg.ExpectedVersion);
            var totalSeconds = watch.Elapsed.TotalSeconds;
            Log.Info("Import completed in {0}sec. That's {1} m/s", totalSeconds, Math.Round(count / totalSeconds));
            msg.Envelope(new ClientMessage.ImportEventsCompleted());
        }

        public void Handle(SystemMessage.Init message)
        {
            Log.Info("Storage starting");
            try
            {
                _store = _func();
                _publisher.Publish(new SystemMessage.StorageWriterInitializationDone());
            }
            catch (Exception ex)
            {
                Application.Exit(ExitCode.Error, "Failed to initialize store: " + ex.Message);
            }
        }
    }
}