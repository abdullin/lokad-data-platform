using System.Reflection;
using System.Text;
using Platform.CommandLine;
using System.Linq;

namespace Platform.Node
{
    class NodeOptions : CommandLineOptionsBase
    {
        [Option("k", "killswitch", DefaultValue = -1, HelpText = "Kill server in seconds")]
        public int KillSwitch { get; set; }

        [Option("h", "http-port", DefaultValue = 8080, HelpText = "Http Port to use")]
        public int HttpPort { get; set; }
        [Option("s", "store", DefaultValue = @"C:\LokadData\dp-store", HelpText = "Location of data store to use")]
        public string StoreLocation { get; set; }
    }
}