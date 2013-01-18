using System;
using System.AddIn;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Platform.MafAddInView;

namespace AddInMyEmailSender
{
    [AddIn("My email sender", Version = "1.0.0.0")]
    public class MyEmailSenderAddIn : MafRun
    {
        public override int MaxBatchSize
        {
            get { return 100; }
        }

        public override string Name
        {
            get { return "email-sender"; }
        }

        public override void Execute(IEnumerable<byte> messsage)
        {
            
        }
    }
}
