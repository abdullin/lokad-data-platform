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
    /// data streams).
    /// </summary>
    public class PlatformClient
    {
        /// <summary>
        /// Creates a connection to event store, which can both read and write events.
        /// </summary>
        /// <param name="storageConfiguration">Storage configuration (either local file path
        ///     or <see cref="AzureStoreConfiguration"/>).</param>
        /// <param name="storeId">Id of the store to connect to</param>
        /// <param name="platformServerEndpoint">URL of public server API.</param>
        /// <returns>new instance of the client that can read and write events.</returns>
        public static IRawEventStoreClient ConnectToEventStore(string storageConfiguration, string storeId, string platformServerEndpoint)
        {
            var container = EventStoreId.Create(storeId);

            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storageConfiguration,out configuration))
            {
                return new FileEventStoreClient(storageConfiguration,container, platformServerEndpoint);
            }
            return new AzureEventStoreClient(configuration, container, platformServerEndpoint);
        }
        /// <summary>
        /// Creates a connection to event store, which can only both read and write events.
        /// Platform API connection is not needed.
        /// </summary>
        /// <param name="storageConfiguration">Storage configuration (either local file path
        /// or <see cref="AzureStoreConfiguration"/>)</param>
        /// <param name="storeId">Id of the store to connect to</param>
        /// <returns>new instance of the client that can read events</returns>
        public static IRawEventStoreClient ConnectToEventStoreAsReadOnly(
            string storageConfiguration, 
            string storeId)
        {
            var container = EventStoreId.Create(storeId);
            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storageConfiguration, out configuration))
            {
                return new FileEventStoreClient(storageConfiguration, container);
            }
            return new AzureEventStoreClient(configuration, container);
        }

        /// <summary>
        /// Creates a connection to view storage
        /// </summary>
        /// <param name="storageConfiguration">Storage configuration (either local file path
        /// or <see cref="AzureStoreConfiguration"/>)</param>
        /// <param name="containerName">container name (directory) where to put views</param>
        /// <returns>new instance of the client that can read and write events</returns>
        public static ViewClient ConnectToViewStorage(string storageConfiguration, string containerName)
        {
            AzureStoreConfiguration configuration;
            if (!AzureStoreConfiguration.TryParse(storageConfiguration, out configuration))
            {
                var root = new FileViewRoot(new DirectoryInfo(storageConfiguration));

                var viewClient = new ViewClient(root.GetContainer(containerName), FileActionPolicy);
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