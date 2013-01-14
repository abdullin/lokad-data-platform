using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Platform.StreamClients;
using Platform.ViewClients;

namespace Platform
{
    /// <summary>
    /// Entry point for configuring access to the core of DataPlatform (both views and 
    /// data streams)
    /// </summary>
    public class PlatformClient
    {
        public static IInternalStreamClient GetStreamReaderWriter(string storage, string serverEndpoint, string containerName = ContainerName.Default)
        {
            var container = ContainerName.Create(containerName);

            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storage,out configuration))
            {
                return new FileStreamClient(storage,container, serverEndpoint);
            }
            return new AzureStreamClient(configuration, container, serverEndpoint);
        }
        public static IInternalStreamClient GetStreamReader(string storage, string containerName = ContainerName.Default)
        {
            var container = ContainerName.Create(containerName);
            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storage, out configuration))
            {
                return new FileStreamClient(storage, container);
            }
            return new AzureStreamClient(configuration, container);
        }

        public static ViewClient GetViewClient(string storage, string containerName)
        {
            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storage, out configuration))
            {
                var container = new FileViewContainer(new DirectoryInfo(storage));

                var viewClient = new ViewClient(container.GetContainer(containerName), FileActionPolicy);
                viewClient.CreateContainer();
                return viewClient;
            }
            else
            {
                var account = CloudStorageAccount.Parse(configuration.ConnectionString);
                var client = account.CreateCloudBlobClient();
                var viewContainer = new AzureViewRoot(client).GetContainer(configuration.Container);

                var viewClient = new ViewClient(viewContainer.GetContainer(containerName), AzureActionPolicy);
                viewClient.CreateContainer();
                return viewClient;
            }
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