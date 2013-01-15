using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Platform.StreamStorage.Azure
{
    public static class StorageExtensions
    {
        public static CloudPageBlob GetPageBlob(this AzureStoreConfiguration config, string blobAddress)
        {
            var account = CloudStorageAccount.Parse(config.ConnectionString);
            var client = account.CreateCloudBlobClient();
            var path = config.RootBlobContainerName + "/" + blobAddress.TrimStart('/');
            return client.GetPageBlobReference(path);
        }
    }
}