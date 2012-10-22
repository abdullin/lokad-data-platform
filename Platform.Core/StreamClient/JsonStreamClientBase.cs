using System;
using Platform.Messages;
using ServiceStack.ServiceClient.Web;

namespace Platform.StreamClient
{
    public abstract class JsonStreamClientBase
    {
        public  JsonServiceClient Client;

        public JsonStreamClientBase(string uri)
        {
            if (!string.IsNullOrWhiteSpace(uri))
            {
                Client = new JsonServiceClient(uri);
            }

        }

        protected void ImportEventsInternal(string streamName, string location)
        {
            ThrowIfClientNotInitialized();
            var response = Client.Post<ClientDto.WriteBatchResponse>(ClientDto.WriteBatch.Url, new ClientDto.WriteBatch()
                {
                    Location = location,
                    Stream = streamName,
                });

            if (!response.Success)
                throw new InvalidOperationException(response.Result ?? "Client error");
        }

        public void WriteEvent(string streamName, byte[] data)
        {
            ThrowIfClientNotInitialized();
            var response = Client.Post<ClientDto.WriteEventResponse>(ClientDto.WriteEvent.Url, new ClientDto.WriteEvent()
            {
                Data = data,
                Stream = streamName
            });
            if (!response.Success)
                throw new InvalidOperationException(response.Result ?? "Client error");
        }

        void ThrowIfClientNotInitialized()
        {
            if (null == Client)
            {
                throw new InvalidOperationException("Client was not initialized");
            }
        }
    }
}