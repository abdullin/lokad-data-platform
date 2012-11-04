using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Platform.Messages;

namespace Platform.Node.Services.Storage
{
    public sealed class FileStorageService :
        IHandle<ClientMessage.AppendEvents>,
        IHandle<SystemMessage.Init>,
        IHandle<ClientMessage.ImportEvents>,
        IHandle<ClientMessage.RequestStoreReset>
    {
        readonly static ILogger Log = LogManager.GetLoggerFor<FileStorageService>();
        readonly IPublisher _publisher;

        FileLogManager _store;

        readonly string _location;
        
        public FileStorageService(string location, IPublisher publisher)
        {
            _location = location;
            _publisher = publisher;
        }

        public void Handle(ClientMessage.AppendEvents message)
        {
            _store
                .GetOrAddStore(message.EventStream)
                .Append(message.EventStream, new[] { message.Data });

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
            Log.Info("Got import request: '{0}'", msg.StagingLocation);
            var watch = Stopwatch.StartNew();
            var count = 0;
            var size = 0;


            var lazy = EnumerateStaging(msg.StagingLocation);
            _store.GetOrAddStore(msg.EventStream).Append(msg.EventStream, lazy.Select(bytes =>
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
                _store = new FileLogManager(_location);
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

    public sealed class FileLogManager
    {
        readonly string _folder;
        static readonly ILogger Log = LogManager.GetLoggerFor<FileLogManager>();

        readonly Dictionary<string,FileAppendOnlyStore> _topics = new Dictionary<string, FileAppendOnlyStore>();
 
        public FileLogManager(string folder)
        {
            _folder = folder;

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var directories = Directory.GetDirectories(folder, "topic-*");
            foreach (var directory in directories)
            {
                try
                {
                    // dead-simple topic naming for now
                    string topic = directory.Remove(0, 6);


                    _topics.Add(topic, new FileAppendOnlyStore(Path.Combine(folder, directory)));
                }
                catch (Exception ex)
                {
                    Log.ErrorException(ex, "Failed to mount '{0}'", directory);
                }
            }
        }

        public FileAppendOnlyStore GetOrAddStore(string topic)
        {
            FileAppendOnlyStore value;
            if (!_topics.TryGetValue(topic,out value))
            {
                value = new FileAppendOnlyStore(Path.Combine(_folder, "topic-" + topic));
                _topics.Add(topic, value);
            }

            return value;
        }
        public void Reset()
        {
            foreach (var source in _topics.ToArray())
            {
                source.Value.Reset();
                _topics.Remove(source.Key);
            }
        }
    }
}