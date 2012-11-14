using System;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Platform.Storage.Azure
{
    public static class StorageExtensions
    {
        public static CloudPageBlob GetPageBlob(this AzureStoreConfiguration config, string blobAddress)
        {
            var account = CloudStorageAccount.Parse(config.ConnectionString);
            var client = account.CreateCloudBlobClient();
            var path = config.Container + "/" + blobAddress.TrimStart('/');
            return client.GetPageBlobReference(path);
        }

        public static CloudBlockBlob GetBlockBlob(this AzureStoreConfiguration config, string blobAddress)
        {
            var account = CloudStorageAccount.Parse(config.ConnectionString);
            var client = account.CreateCloudBlobClient();
            var path = config.Container + "/" + blobAddress.TrimStart('/');
            return client.GetBlockBlobReference(path);
        }

        public static bool Exists(this CloudBlob blob)
        {
            try
            {
                blob.FetchAttributes();
                return true;
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                    return false;

                throw;
            }
        }
    }
}