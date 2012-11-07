using System;
using System.Globalization;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;

namespace Platform.Storage.Azure
{
    public static class StorageExtensions
    {
        const string CommittedSizeName = "committedsize";

        public static long GetCommittedSize(this CloudBlob blob)
        {
            blob.FetchAttributes();
            return Int64.Parse(blob.Metadata[CommittedSizeName] ?? "0");
        }

        public static void SetCommittedSize(this CloudBlob blob, long size)
        {
            blob.Metadata[CommittedSizeName] = size.ToString(CultureInfo.InvariantCulture);
            blob.SetMetadata();
        }

        public static CloudPageBlob GetPageBlobReference(string connectionString, string blobAddress)
        {
            var client = GetCloudBlobClient(connectionString);
            return client.GetPageBlobReference(blobAddress);
        }

        public static CloudBlobClient GetCloudBlobClient(string connectionString)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudBlobClient();
            return client;
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

        public static bool Exists(this CloudBlobContainer blob)
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

        public static void SetLength(this CloudPageBlob blob, long newLength, int timeout = 10000)
        {
            var credentials = blob.ServiceClient.Credentials;

            var requestUri = blob.Uri;
            if (credentials.NeedsTransformUri)
                requestUri = new Uri(credentials.TransformUri(requestUri.ToString()));

            var request = BlobRequest.SetProperties(requestUri, timeout, blob.Properties, null, newLength);
            request.Timeout = timeout;

            credentials.SignRequest(request);

            using (request.GetResponse()) { }
        }
    }
}