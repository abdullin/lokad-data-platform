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
        public static IRawEventStoreClient GetEventStoreReaderWriter(string storage, 
            string serverEndpoint, string storeId = EventStoreId.Default)
        {
            var container = EventStoreId.Create(storeId);

            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storage,out configuration))
            {
                return new FileEventStoreClient(storage,container, serverEndpoint);
            }
            return new AzureEventStoreClient(configuration, container, serverEndpoint);
        }

        public static IRawEventStoreClient GetStreamReader(string storage, string containerName = EventStoreId.Default)
        {
            var container = EventStoreId.Create(containerName);
            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storage, out configuration))
            {
                return new FileEventStoreClient(storage, container);
            }
            return new AzureEventStoreClient(configuration, container);
        }

        public static ViewClient GetViewClient(string storage, string containerName)
        {
            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storage, out configuration))
            {
                var container = new FileViewContainer(new DirectoryInfo(storage));

                var viewClient = new ViewClient(container.GetContainer(containerName), FileActionPolicy);
                viewClient.CreateContainerIfNeeded();
                return viewClient;
            }
            else
            {
                var account = CloudStorageAccount.Parse(configuration.ConnectionString);
                var client = account.CreateCloudBlobClient();
                var viewContainer = new AzureViewRoot(client).GetContainer(configuration.RootBlobContainerName);

                var viewClient = new ViewClient(viewContainer.GetContainer(containerName), AzureActionPolicy);
                viewClient.CreateContainerIfNeeded();
                return viewClient;
            }
        }

        /// <summary>
        /// Retry policy to deal with transient errors on filesystem.
        /// Defines when to give up on retry.
        /// </summary>
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

        /// <summary>
        /// Retry policy to deal with transient errors on Windows Azure Storage.
        /// Defines when to give up on retry.
        /// </summary>
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