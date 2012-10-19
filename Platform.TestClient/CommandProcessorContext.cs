using System;
using System.Threading;

namespace Platform.TestClient
{
    public class CommandProcessorContext
    {
        public readonly Client Client;
        public ILogger Log;
        
        public CommandProcessorContext(Client client, ILogger log)
        {
            Log = log;
            Client = client;
        }
    }

}