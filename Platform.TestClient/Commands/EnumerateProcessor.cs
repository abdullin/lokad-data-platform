using System.Diagnostics;

namespace Platform.TestClient.Commands
{
    public class EnumerateProcessor : ICommandProcessor
    {
        public string Key { get { return "EN"; } }
        public string Usage { get { return "EN [maxcount]"; } }
        public bool Execute(CommandProcessorContext context, string[] args)
        {
            var maxCount = 10000;

            if (args.Length > 0)
                int.TryParse(args[0], out maxCount);

            var result = context.Client.Platform.ReadAll(0, maxCount);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int msgCount = 0;
            foreach (var retrievedDataRecord in result)
            {
                msgCount++;
            }
            sw.Stop();
            context.Log.Info("{0} messages per second", (int)(msgCount / sw.Elapsed.TotalSeconds));
            PerfUtils.LogTeamCityGraphData(string.Format("EN_{0}_msgPerSeq", maxCount), (int)(msgCount / sw.Elapsed.TotalSeconds));
            return true;
        }
    }
}