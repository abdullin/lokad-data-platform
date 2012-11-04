using System.Diagnostics;
using System.Threading;
using Platform.StreamClients;

namespace Platform.TestClient.Commands
{
//    public class EnumerateProcessor : ICommandProcessor
//    {
//        public string Key { get { return "EN"; } }
//        public string Usage { get { return @"EN [maxcount]
//    Scans through up to <maxcount> records from the beginning of the stream"; } }
//        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
//        {
//            var maxCount = 10000;

//            if (args.Length > 0)
//                int.TryParse(args[0], out maxCount);
//            var sw = Stopwatch.StartNew();
//            var records = context.Client.Streams.ReadAll(StorageOffset.Zero, maxCount);
//            int msgCount = 0;
//            long dataSize = 0;
//            foreach (var record in records)
//            {
//                msgCount += 1;
//                dataSize += record.Data.Length;
//            }
            
//            sw.Stop();
//            var speed = dataSize / sw.Elapsed.TotalSeconds;
//            var messageSpeed = (int) (msgCount / sw.Elapsed.TotalSeconds);
//            context.Log.Info("{0} msgPerSec or {1}", messageSpeed, FormatEvil.SpeedInBytes(speed));
//            PerfUtils.LogTeamCityGraphData(string.Format("EN_{0}_msgPerSec", maxCount), messageSpeed);
//            PerfUtils.LogTeamCityGraphData(string.Format("EN_{0}_bytesPerSec",maxCount),(int) speed);
//            return true;
//        }
//    }
}