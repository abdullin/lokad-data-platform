using System;
using System.Threading;

namespace Platform.TestClient.Commands.Bench
{
    public class BasicBenchmarkProcessor : ICommandProcessor
    {
        public string Key { get { return "BENCH1"; } }
        public string Usage { get { return "BENCH1"; } }

        static string[] Args(string data = null)
        {
            if (string.IsNullOrWhiteSpace(data))
                return new string[0];
            return data.Split(' ');
        }

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            try
            {
                var success = true;
                success &= new ResetStoreProcessor().Execute(context, token, Args());
                success &= new StartLocalServerProcessor().Execute(context, token, Args("-k 300"));
                success &= new WriteEventsFloodProcessor().Execute(context, token, Args("5 20"));

                success &= new WriteEventsFloodProcessor().Execute(context, token, Args("10 10"));
                success &= new WriteEventsFloodProcessor().Execute(context, token, Args("2 50"));
                success &= new BasicTestProcessor().Execute(context, token, Args("10 10000 10 20"));
                return success;
            }
            catch (Exception ex)
            {
                context.Log.ErrorException(ex, "Unhandled client exception: {0}", ex.Message);
                return false;
            }
            finally
            {
                new ShutdownProcessor().Execute(context, token, new string[0]);

            }
        }
    }
}