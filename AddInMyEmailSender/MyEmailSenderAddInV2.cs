#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 
// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence
#endregion

using System.AddIn;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Platform.MafAddInView;

namespace AddInMyEmailSender
{
    [AddIn("My email sender", Version = "1.1.0.0")]
    public class MyEmailSenderAddInV2 : MafRun
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
            EmailSender.EmailSender.Send(string.Format("Email sender(v.1.1): {0}", Encoding.UTF8.GetString(messsage.ToArray())));
        }
    }
}