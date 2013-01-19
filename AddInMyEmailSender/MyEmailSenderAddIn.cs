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

        public override string[] FilteredStreamIds
        {
            get { return new[] { "send-email" }; }
        }

        public override void Execute(IEnumerable<byte> messsage)
        {
            EmailSender.EmailSender.Send(string.Format("Email sender(v.1): {0}", Encoding.UTF8.GetString(messsage.ToArray())));
        }
    }
}
