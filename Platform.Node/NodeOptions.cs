using Platform.CommandLine;

namespace Platform.Node
{
    /// <summary>
    /// Command line options for the server
    /// </summary>
    public class NodeOptions : CommandLineOptionsBase
    {
        [Option("k", "killswitch", DefaultValue = -1, HelpText = "Kill server in seconds")]
        public int KillSwitch { get; set; }

        [Option("h", "http-port", DefaultValue = 8080, HelpText = "Http Port to use")]
        public int HttpPort { get; set; }
        [Option("s", "store", DefaultValue = @"C:\LokadData\dp-store", HelpText = "Location of data store to use")]
        public string StoreLocation { get; set; }
    }
}