using System;
using System.Threading;

namespace Platform.TestClient
{
    public class CommandProcessorContext
    {
        public readonly Client Client;
        public ILogger Log;
        public readonly CancellationTokenSource Token;
        
        public CommandProcessorContext(Client client, ILogger log, CancellationTokenSource token)
        {
            Log = log;
            Client = client;
            Token = token;
        }

        //public void IsAsync()
        //{
        //    _resetEvent.Reset();
        //}

        //public void Completed()
        //{
        //    _resetEvent.Set();
        //}

        //public void WaitForCompletion()
        //{
        //    if (Client.Options.Timeout <= 0)
        //        _resetEvent.WaitOne();
        //    else
        //    {
        //        if (!_resetEvent.WaitOne(Client.Options.Timeout * 1000))
        //        {
        //            var s = string.Format("Command didn't finished within timeout of {0} sec.", Client.Options.Timeout);
        //            throw new TimeoutException(s);
        //        }
        //    }
        //}
    }


}