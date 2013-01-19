using System;
using System.AddIn.Hosting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Platform;
using Platform.MafHVA;
using Platform.StreamClients;
using Platform.ViewClients;

namespace Host
{
    class Program
    {
        static IRawEventStoreClient _reader;
        static ViewClient _views;
        static Dictionary<string, DateTime> _lastAddInChangeInfo;
        static bool _addInIsChange;
        static string _pipelineRootFolderPath;
        static IEnumerable<AddInToken> _tokens;
        static int _runingDllCount;

        static void Main(string[] args)
        {
            var storePath = ConfigurationManager.AppSettings["StorePath"];
            var storeConnection = ConfigurationManager.AppSettings["StoreConnection"];
            _reader = PlatformClient.ConnectToEventStore(storePath, "default", storeConnection);
            _views = PlatformClient.ConnectToViewStorage(storePath, "host-run-views");
            // Assume that the current directory is the application folder,  
            // and that it contains the pipeline folder structure. 
            _pipelineRootFolderPath = Environment.CurrentDirectory + "\\Pipeline";
            _tokens = new AddInToken[0];
            _lastAddInChangeInfo = new Dictionary<string, DateTime>();

            ThreadPool.QueueUserWorkItem(x =>
            {
                while (true)
                {
                    _addInIsChange = AddInIsChange(_pipelineRootFolderPath);
                    if (_addInIsChange)
                    {
                        while (_runingDllCount > 0)
                            Thread.Sleep(100);

                        _addInIsChange = false;
                        RebuildRun();
                    }

                    Thread.Sleep(5000);
                }
            }, Thread.CurrentThread);

            Console.ReadLine();
        }

        static void RebuildRun()
        {
            _runingDllCount = 0;

            //Check to see if new add-ins have been installed.
            AddInStore.Rebuild(_pipelineRootFolderPath);

            _tokens = GetLastVersionToken(_pipelineRootFolderPath);

            foreach (AddInToken token in _tokens)
            {
                Task.Factory.StartNew(() =>
                    {
                        Interlocked.Increment(ref _runingDllCount);
                        var run = token.Activate<MafRun>(AddInSecurityLevel.FullTrust);

                        while (!_addInIsChange)
                        {
                            Running(run);
                        }

                        // shutdown the add-in when the IRun method finishes executing
                        AddInController controller = AddInController.GetAddInController(run);
                        controller.Shutdown();

                        Interlocked.Decrement(ref _runingDllCount);
                    },
                    TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
            }
        }

        static bool AddInIsChange(string addInRoot)
        {
            bool addInChange = false;
            var dllFiles = new DirectoryInfo(addInRoot).GetFiles("*.dll", SearchOption.AllDirectories);

            List<string> oldFiles = _lastAddInChangeInfo.Select(x => x.Key).ToList();
            foreach (var dllFile in dllFiles)
            {
                if (_lastAddInChangeInfo.ContainsKey(dllFile.FullName))
                    oldFiles.Remove(dllFile.FullName);

                if (!_lastAddInChangeInfo.ContainsKey(dllFile.FullName) ||
                    _lastAddInChangeInfo[dllFile.FullName] != dllFile.LastWriteTimeUtc)
                {
                    addInChange = true;
                    _lastAddInChangeInfo[dllFile.FullName] = dllFile.LastWriteTimeUtc;
                }
            }

            if (oldFiles.Count > 0)
                addInChange = true;
            foreach (string oldFile in oldFiles)
                _lastAddInChangeInfo.Remove(oldFile);

            return addInChange;
        }

        static void Running(MafRun run)
        {
            var checkpoint = _views.ReadAsJsonOrGetNew<RunCheckpoint>(run.Name);

            var events = _reader.ReadAllEvents(new EventStoreOffset(checkpoint.NextOffset), run.MaxBatchSize);
            bool executeAllEvents = run.FilteredStreamIds == null || run.FilteredStreamIds.Length == 0;

            foreach (var @event in events)
            {
                checkpoint.NextOffset = @event.Next.OffsetInBytes;
                if (executeAllEvents || run.FilteredStreamIds.Contains(@event.StreamId))
                    run.Execute(@event.EventData);
            }
            _views.WriteAsJson(checkpoint, run.Name);
        }

        static IEnumerable<AddInToken> GetLastVersionToken(string addInRoot)
        {
            Collection<AddInToken> tokens = AddInStore.FindAddIns(typeof(MafRun), addInRoot);

            for (var i = 0; i < tokens.Count - 1; i++)
            {
                var token = tokens[i];
                for (var j = i + 1; j < tokens.Count; j++)
                {
                    if (token.Name != tokens[j].Name)
                        continue;

                    var versionI = new Version(token.Version);
                    var versionJ = new Version(tokens[j].Version);
                    if (versionI > versionJ)
                    {
                        tokens.RemoveAt(j);
                        j--;
                        continue;
                    }
                    if (versionI < versionJ)
                    {
                        tokens.RemoveAt(i);
                        i--;
                        break;
                    }
                    throw new Exception(string.Format("two libraries with the same version"));
                }
            }

            return tokens;
        }

        public class RunCheckpoint
        {
            public long NextOffset { get; set; }
        }
    }
}
