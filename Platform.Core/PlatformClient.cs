using System.IO;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Platform.Storage;
using Platform.Storage.Azure;

namespace Platform
{
    public class PlatformClient
    {
        public static IInternalStreamClient StreamClient(string storage, string serverEndpoint)
        {
            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storage,out configuration))
            {
                return new FilePlatformClient(storage,serverEndpoint);
            }
            return new AzurePlatformClient(configuration, serverEndpoint);
        }

        public static IViewContainer ViewClient(string storage)
        {
            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storage, out configuration))
            {
                return new FileViewContainer(new DirectoryInfo(storage));
            }
            var account = CloudStorageAccount.Parse(configuration.ConnectionString);
            var client = account.CreateCloudBlobClient();
            return new BlobViewRoot(client).GetContainer(configuration.Container);
        }




    }
}