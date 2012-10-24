using System.Net;
using Platform.Messages;
using ServiceStack.ServiceClient.Web;

namespace Platform.StreamClients
{
    public abstract class JsonStreamClientBase
    {
        public  JsonServiceClient WriteClient;

        protected JsonStreamClientBase(string uri)
        {
            if (!string.IsNullOrWhiteSpace(uri))
            {
                WriteClient = new JsonServiceClient(uri);
            }
        }

        public const int MessageSizeLimit = 1024 * 1024 * 2;

        protected void ImportEventsInternal(string streamName, string location)
        {
            ThrowIfClientNotInitialized();
            try
            {
                var response = WriteClient.Post<ClientDto.WriteBatchResponse>(ClientDto.WriteBatch.Url,
                    new ClientDto.WriteBatch
                        {
                            Location = location,
                            Stream = streamName,
                        });
                if (!response.Success)
                {
                    throw new PlatformClientException(response.Result ?? "Server error");
                }
            }
            catch (WebException ex)
            {
                var message = string.Format("Connection failure: {0}", ex.Status);
                throw new PlatformClientException(message, ex);
            }
        }

        public void WriteEvent(string streamName, byte[] data)
        {
            ThrowIfClientNotInitialized();
            try
            {
                var response = WriteClient.Post<ClientDto.WriteEventResponse>(ClientDto.WriteEvent.Url,
                    new ClientDto.WriteEvent
                        {
                            Data = data,
                            Stream = streamName
                        });
                if (!response.Success)
                {
                    throw new PlatformClientException(response.Result ?? "Server error");
                }
            }
            catch(WebException ex)
            {
                var message = string.Format("Connection failure: {0}", ex.Status);
                throw new PlatformClientException(message, ex);   
            }
        }

        void ThrowIfClientNotInitialized()
        {
            if (null != WriteClient) return;
            throw new PlatformClientException("This client is read-only");
        }
    }
}