using System;
using System.AddIn.Pipeline;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Platform.MafAddInView
{
    [AddInBase]
    public abstract class MafRun
    {
        public abstract int MaxBatchSize { get; }
        public abstract string Name { get; }
        public abstract string[] FilteredStreamIds { get; }
        public abstract void Execute(IEnumerable<byte> messsage);
    }
}
