using System;
using System.AddIn.Contract;
using System.AddIn.Pipeline;
using System.Collections.Generic;

namespace Platform.MafContract
{
    [AddInContract]
    public interface IRunContract : IContract
    {
        int MaxBatchSize { get; }
        string Name { get;}
        void Execute(IEnumerable<byte> messsage);
    }
}