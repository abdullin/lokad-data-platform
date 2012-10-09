using System;
using System.Threading;

namespace Platform.TestClient
{
    public class CommandProcessorContext
    {
        public readonly Client Client;
        public ILogger Log;

        private readonly ManualResetEvent _resetEvent;

        public CommandProcessorContext(Client client, ILogger log, ManualResetEvent resetEvent)
        {
            Log = log;
            Client = client;
            _resetEvent = resetEvent;
        }

        public void IsAsync()
        {
            _resetEvent.Reset();
        }

        public void Completed()
        {
            _resetEvent.Set();
        }

        public void WaitForCompletion()
        {
            if (Client.Options.Timeout <= 0)
                _resetEvent.WaitOne();
            else
            {
                if (!_resetEvent.WaitOne(Client.Options.Timeout * 1000))
                    throw new TimeoutException("Command didn't finished within timeout.");
            }
        }
    }
}