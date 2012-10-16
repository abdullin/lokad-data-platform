using System;
using Platform.Messages;
using ServiceStack.ServiceClient.Web;

namespace Platform
{
    public abstract class JsonPlatformClientBase
    {
        public  JsonServiceClient Client;

        public JsonPlatformClientBase(string uri)
        {
            if (!string.IsNullOrWhiteSpace(uri))
            {
                Client = new JsonServiceClient(uri);
            }

        }

        protected void ImportEventsInternal(string streamName, string location)
        {
            var response = Client.Post<ClientDto.ImportEventsResponse>("/import", new ClientDto.ImportEvents()
                {
                    Location = location,
                    Stream = streamName,
                });

            if (!response.Success)
                throw new InvalidOperationException(response.Result ?? "Client error");
        }

        public void WriteEvent(string streamName, byte[] data)
        {
            var response = Client.Post<ClientDto.WriteEventResponse>("/stream", new ClientDto.WriteEvent()
            {
                Data = data,
                Stream = streamName
            });
            if (!response.Success)
                throw new InvalidOperationException(response.Result ?? "Client error");
        }


    }
}