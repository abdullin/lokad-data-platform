using System.Collections.Generic;

namespace Platform.TestClient
{
    public sealed class ClientOptions
    {
        public string Ip { get; set; }
        public string HttpPort { get; set; }
        public IList<string> Command { get; set; }
        public int Timeout { get; set; }
    }
}