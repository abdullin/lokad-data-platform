using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Platform.Storage;
using Platform.Storage.Azure;
using System.Linq;

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

        //public static IViewContainer ViewClient(string storage)
        //{
        //    AzureStoreConfiguration configuration;
        //    if (!AzureStoreConfiguration.TryParse(storage, out configuration))
        //    {
        //        return new FileViewContainer(new DirectoryInfo(storage));
        //    }
        //    var account = CloudStorageAccount.Parse(configuration.ConnectionString);
        //    var client = account.CreateCloudBlobClient();
        //    return new BlobViewRoot(client).GetContainer(configuration.Container);
        //}

        public static ViewClient ViewClient(string storage, string containerName)
        {
            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storage, out configuration))
            {
                var container = new FileViewContainer(new DirectoryInfo(storage));
                return new ViewClient(container.GetContainer(containerName), FileActionPolicy);
            }
            var account = CloudStorageAccount.Parse(configuration.ConnectionString);
            var client = account.CreateCloudBlobClient();
            var viewContainer = new BlobViewRoot(client).GetContainer(configuration.Container);

            return new ViewClient(viewContainer.GetContainer(containerName), AzureActionPolicy);
        }

        static bool FileActionPolicy(Queue<Exception> exceptions)
        {
            if (exceptions.Count >= 4)
                return true;

            var ex = exceptions.Peek();


            if (!(ex is IOException))
                return true;

            Thread.Sleep(200 * exceptions.Count);
            return false;
        }
        static bool AzureActionPolicy(Queue<Exception> exceptions)
        {
            if (exceptions.Count >= 4)
                return true;

            var ex = exceptions.Peek();


            if (!(ex is StorageException))
                return true;

            Thread.Sleep(200 * exceptions.Count);
            return false;
        }
    }
}