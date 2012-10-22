using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Platform.StreamClient;
using Platform.ViewClient;

namespace Platform
{
    public class PlatformClient
    {
        public static IInternalStreamClient GetStreamReaderWriter(string storage, string serverEndpoint)
        {
            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storage,out configuration))
            {
                return new FileStreamClient(storage,serverEndpoint);
            }
            return new AzureStreamClient(configuration, serverEndpoint);
        }
        public static IInternalStreamClient GetStreamReader(string storage)
        {
            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storage, out configuration))
            {
                return new FileStreamClient(storage);
            }
            return new AzureStreamClient(configuration);
        }

        public static ViewClient.ViewClient GetViewClient(string storage, string containerName)
        {
            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storage, out configuration))
            {
                var container = new FileViewContainer(new DirectoryInfo(storage));
                return new ViewClient.ViewClient(container.GetContainer(containerName), FileActionPolicy);
            }
            var account = CloudStorageAccount.Parse(configuration.ConnectionString);
            var client = account.CreateCloudBlobClient();
            var viewContainer = new BlobViewRoot(client).GetContainer(configuration.Container);

            return new ViewClient.ViewClient(viewContainer.GetContainer(containerName), AzureActionPolicy);
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