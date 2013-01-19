using System;
using System.AddIn.Hosting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
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

        static void Main(string[] args)
        {
            var storePath = ConfigurationManager.AppSettings["StorePath"];
            var storeConnection = ConfigurationManager.AppSettings["StoreConnection"];
            _reader = PlatformClient.ConnectToEventStore(storePath, "default", storeConnection);
            _views = PlatformClient.ConnectToViewStorage(storePath, "host-run-views");

            RebuildRun();
        }

        static void RebuildRun()
        {
            // Assume that the current directory is the application folder,  
            // and that it contains the pipeline folder structure. 
            var addInRoot = Environment.CurrentDirectory + "\\Pipeline";

            //Check to see if new add-ins have been installed.
            AddInStore.Rebuild(addInRoot);

            IEnumerable<AddInToken> tokens = GetLastVersionToken(addInRoot);

            bool changeDllFolders = false;

            Parallel.ForEach(tokens, token =>
                {
                    var run = token.Activate<MafRun>(AddInSecurityLevel.FullTrust);

                    while (!changeDllFolders)
                    {
                        Running(run);
                    }
                });

            if (changeDllFolders)
                RebuildRun();
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
