using System.Threading;

namespace Platform.TestClient
{
    public class CommandProcessorContext
    {
        public readonly Client Client;
        private readonly ManualResetEvent _resetEvent;

        public CommandProcessorContext(Client client,ManualResetEvent resetEvent)
        {
            Client = client;
            _resetEvent = resetEvent;
        }

        public void IsAsync()
        {
            _resetEvent.Reset();
        }
    }
}