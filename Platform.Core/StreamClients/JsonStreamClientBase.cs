using System.Net;
using ServiceStack.ServiceClient.Web;

namespace Platform.StreamClients
{
    public abstract class JsonStreamClientBase
    {
        public  JsonServiceClient WriteClient;
        public readonly EventStoreId Container;

        protected JsonStreamClientBase(EventStoreId container, string uri)
        {
            Container = container;
            if (!string.IsNullOrWhiteSpace(uri))
            {
                WriteClient = new JsonServiceClient(uri);
            }
        }

        public const int MessageSizeLimit = 1024 * 1024 * 2;

        protected void ImportEventsInternal(string streamId, string location, long length)
        {
            ThrowIfClientNotInitialized();
            try
            {
                var response = WriteClient.Post<ClientDto.WriteBatchResponse>(ClientDto.WriteBatch.Url,
                    new ClientDto.WriteBatch
                        {
                            Container = Container.Name,
                            StreamKey = streamId,
                            Location = location,
                            Length = length
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

        public void WriteEvent(string streamId, byte[] eventData)
        {
            ThrowIfClientNotInitialized();
            try
            {
                var response = WriteClient.Post<ClientDto.WriteEventResponse>(ClientDto.WriteEvent.Url,
                    new ClientDto.WriteEvent
                        {
                            Container = Container.Name,
                            StreamKey = streamId,
                            Data = eventData,
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