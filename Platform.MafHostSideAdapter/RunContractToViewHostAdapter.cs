using System;
using System.AddIn.Pipeline;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Platform.MafContract;
using Platform.MafHVA;

namespace Platform.MafHostSideAdapter
{
    [HostAdapter]
    public class RunContractToViewHostAdapter : MafRun
    {
        private IRunContract _contract;

        private System.AddIn.Pipeline.ContractHandle _handle;

        public RunContractToViewHostAdapter(IRunContract contract)
        {
            _contract = contract;
            _handle = new System.AddIn.Pipeline.ContractHandle(contract);
        }

        public override int MaxBatchSize
        {
            get { return _contract.MaxBatchSize; }
        }

        public override string Name
        {
            get { return _contract.Name; }
        }

        public override string[] FilteredStreamIds
        {
            get { return _contract.FilteredStreamIds; }
        }

        public override void Execute(IEnumerable<byte> messsage)
        {
            _contract.Execute(messsage);
        }
    }
}
