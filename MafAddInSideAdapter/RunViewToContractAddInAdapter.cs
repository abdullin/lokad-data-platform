using System;
using System.AddIn.Pipeline;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Platform.MafAddInView;
using Platform.MafContract;

namespace MafAddInSideAdapter
{
    [AddInAdapterAttribute]
    public class RunViewToContractAddInAdapter : ContractBase, IRunContract
    {
        MafRun _runView;

        public RunViewToContractAddInAdapter(MafRun runView)
        {
            _runView = runView;
        }

        public int MaxBatchSize { get { return _runView.MaxBatchSize; } }
        public string Name { get { return _runView.Name; } }
        public string[] FilteredStreamIds { get { return _runView.FilteredStreamIds; } }

        public void Execute(IEnumerable<byte> messsage)
        {
            _runView.Execute(messsage);
        }
    }
}
