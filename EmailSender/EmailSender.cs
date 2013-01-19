using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Platform;
using Platform.StreamClients;
using Platform.ViewClients;

namespace EmailSender
{
    public class EmailSender
    {
        static ViewClient _views;

        public static void Send(string message)
        {
            const string storePath = @"C:\LokadData\dp-store";
            _views = PlatformClient.ConnectToViewStorage(storePath, "email-sender-view");

            var oldMessage = _views.ReadAsJsonOrGetNew<List<EmailFormat>>("email");
            oldMessage.Add(new EmailFormat { Date = DateTime.UtcNow, Message = message });

            _views.WriteAsJson(oldMessage, "email");
        }

        class EmailFormat
        {
            public DateTime Date { get; set; }
            public string Message { get; set; }
        }
    }
}
