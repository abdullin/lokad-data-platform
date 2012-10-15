using System.Collections.Generic;
using System.Text;
using Platform.Node;
using Platform.Node.Services.ServerApi;

namespace Platform.TestClient.Commands
{
    public class WriteProccessor : ICommandProcessor
    {
        public string Key { get { return "WR"; } }
        public string Usage { get { return "WR [<stream-id> <expected-version> <data>]"; } }
        public bool Execute(CommandProcessorContext context, string[] args)
        {
            var eventStreamId = "default stream";
            var expectedVersion = -1;
            var data = "default-data";

            if (args.Length > 0)
            {
                if (args.Length != 3)
                {
                    context.Log.Info("More arguments: {0}",args.Length);
                    return false;
                }

                eventStreamId = args[0];
                int.TryParse(args[1], out expectedVersion);
                data = args[2];
            }

            context.IsAsync();

            context.Client.JsonClient.Post<ClientDto.WriteEvent>("/stream", new ClientDto.WriteEvent()
            {
                Data = Encoding.UTF8.GetBytes(data),
                Stream = eventStreamId
            });

            context.Completed();
            return true;
        }
    }
}