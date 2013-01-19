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

namespace Host
{
    class Program
    {
        private static IRawEventStoreClient _reader;

        static void Main(string[] args)
        {
            var storePath = ConfigurationManager.AppSettings["StorePath"];
            var storeConnection = ConfigurationManager.AppSettings["StoreConnection"];
            _reader = PlatformClient.ConnectToEventStore(storePath, "sample3", storeConnection);

            RebuildRun();
        }

        static void RebuildRun()
        {
            // Assume that the current directory is the application folder,  
            // and that it contains the pipeline folder structure. 
            var addInRoot = Environment.CurrentDirectory + "\\Pipeline";

            //Check to see if new add-ins have been installed.
            AddInStore.Rebuild(addInRoot);

            //Search for Calculator add-ins.
            IEnumerable<AddInToken> tokens = GetLastVersionToken(addInRoot);

            bool changeDllFolders = false;

            Parallel.ForEach(tokens, token =>
                {
                    //Activate the selected AddInToken in a new 
                    //application domain with the Internet trust level.
                    var run = token.Activate<MafRun>(AddInSecurityLevel.Internet);

                    //Run the add-in.
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
            int nextOffcet = 0;
            var events = _reader.ReadAllEvents(new EventStoreOffset(nextOffcet), run.MaxBatchSize);

            foreach (var @event in events)
            {
                if (@event.StreamId == run.Name)
                    run.Execute(@event.EventData);
            }
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
                        tokens.RemoveAt(j);
                        i--;
                        break;
                    }
                    throw new Exception(string.Format("two libraries with the same version"));
                }
            }

            return tokens;
        }
    }
}
