using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartApp.Sample3.Contracts
{
    public class Sample3Data
    {
        public long NextOffset { get; set; }
        public int EventsProcessed { get; set; }

        public Dictionary<string, long> Distribution { get; set; }
    }

}
