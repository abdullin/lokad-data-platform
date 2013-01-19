using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Platform.MafHVA
{
    public abstract class MafRun
    {
        public abstract int MaxBatchSize { get; }
        public abstract string Name { get; }
        public abstract string[] FilteredStreamIds { get; }
        public abstract void Execute(IEnumerable<byte> messsage);
    }
}
