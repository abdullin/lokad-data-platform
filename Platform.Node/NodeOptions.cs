using System.Net;
using Platform.CommandLine;

namespace Platform.Node
{
    /// <summary>
    /// Command line options for the server.
    /// </summary>
    public class NodeOptions : CommandLineOptionsBase
    {
        public const int KillSwitchDefault = -1;
        public const int HttpPortDefault = 8080;
        public const string StoreLocationDefault = @"C:\LokadData\dp-store";

        [Option("k", "killswitch", DefaultValue = KillSwitchDefault, HelpText = "Kill server in seconds")]
        public int KillSwitch { get; set; }

        [Option("h", "http-port", DefaultValue = HttpPortDefault, HelpText = "Http Port to use")]
        public int HttpPort { get; set; }

        [Option("i", "ip", DefaultValue = "*", HelpText = "Interface for http endpoint")]
        public string LocalHttpIp { get; set; }

        [Option("s", "store", DefaultValue = StoreLocationDefault, HelpText = "Location of data store to use")]
        public string StoreLocation { get; set; }
    }
}