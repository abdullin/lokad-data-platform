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
            String addInRoot = Environment.CurrentDirectory + "\\Pipeline";

            //Check to see if new add-ins have been installed.
            AddInStore.Rebuild(addInRoot);

            //Search for Calculator add-ins.
            Collection<AddInToken> tokens = AddInStore.FindAddIns(typeof(MafRun), addInRoot);

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

            if(changeDllFolders)
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
    }
}
