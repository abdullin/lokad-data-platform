using System.Collections.Generic;
using Platform.CommandLine;

namespace Platform.TestClient
{
    /// <summary>
    /// Command line options for the client
    /// </summary>
    public sealed class ClientOptions : CommandLineOptionsBase
    {
        [Option("i", "ip", DefaultValue = "localhost", HelpText = "IP address of server")]
        public string Ip { get; set; }
        [Option("h", "http-port", DefaultValue = "8080", HelpText = "HTTP port on server")]
        public string HttpPort { get; set; }
        [ValueList(typeof(List<string>), MaximumElements = -1)]
        public IList<string> Command { get; set; }
        [Option("t", "timeout", DefaultValue = -1, HelpText = "Timeout for command execution in seconds, -1 for infinity")]
        public int Timeout { get; set; }
        [Option("s", "store", DefaultValue = @"C:\LokadData\dp-store", HelpText = "Location of data store to use")]
        public string StoreLocation { get; set; }
        [Option("v","views", DefaultValue = "views", HelpText = "Container to use for views")]
        public string ViewsFolder { get; set; }
    }
}