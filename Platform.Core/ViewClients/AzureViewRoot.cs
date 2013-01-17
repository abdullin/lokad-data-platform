using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.WindowsAzure.StorageClient;

namespace Platform.ViewClients
{
    /// <summary>
    /// Windows Azure implementation of storage 
    /// </summary>
    public sealed class AzureViewRoot : IRawViewRoot
    {
        readonly CloudBlobClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureViewRoot"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        public AzureViewRoot(CloudBlobClient client)
        {
            _client = client;
        }

        public IRawViewContainer GetContainer(string containerName)
        {
            return new AzureViewContainer(_client.GetBlobDirectoryReference(containerName));
        }

        public IEnumerable<string> ListContainers(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return _client.ListContainers().Select(c => c.Name);
            }
            return _client.ListContainers(prefix).Select(c => c.Name);
        }
    }

    /// <summary>
    /// Windows Azure implementation of storage 
    /// </summary>
    public sealed class AzureViewContainer : IRawViewContainer
    {
        readonly CloudBlobDirectory _directory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureViewContainer"/> class.
        /// </summary>
        /// <param name="directory">The directory.</param>
        public AzureViewContainer(CloudBlobDirectory directory)
        {
            _directory = directory;
        }

        public IRawViewContainer GetContainer(string containerName)
        {
            if (containerName == null) throw new ArgumentNullException("containerName");

            return new AzureViewContainer(_directory.GetSubdirectory(containerName));
        }

        public Stream OpenRead(string itemName)
        {
            return _directory.GetBlobReference(itemName).OpenRead();
        }

        public Stream OpenWrite(string itemName)
        {
            return _directory.GetBlobReference(itemName).OpenWrite();
        }

        public void TryDeleteItem(string itemName)
        {
            _directory.GetBlobReference(itemName).DeleteIfExists();
        }

        public bool ItemExists(string itemName)
        {
            try
            {
                _directory.GetBlobReference(itemName).FetchAttributes();
                return true;
            }
            catch (StorageClientException ex)
            {
                return false;
            }
        }


        public IRawViewContainer EnsureContainerExists()
        {
            _directory.Container.CreateIfNotExist();
            return this;
        }

        /// <summary>
        /// Deletes this container
        /// </summary>
        public void DeleteContainer()
        {
            try
            {
                if (_directory.Uri == _directory.Container.Uri)
                {
                    _directory.Container.Delete();
                }
                else
                {
                    _directory.ListBlobs().AsParallel().ForAll(l =>
                    {
                        var name = l.Parent.Uri.MakeRelativeUri(l.Uri).ToString();
                        var r = _directory.GetBlobReference(name);
                        r.BeginDeleteIfExists(ar => { }, null);
                    });
                }
            }
            catch (StorageClientException e)
            {
                switch (e.ErrorCode)
                {
                    case StorageErrorCode.ContainerNotFound:
                        return;
                    default:
                        throw;
                }
            }
        }

        public IEnumerable<string> ListAllNestedItems()
        {
            try
            {
                return _directory.ListBlobs()
                    .Select(item => _directory.Uri.MakeRelativeUri(item.Uri).ToString())
                    .ToArray();
            }
            catch (StorageClientException e)
            {
                switch (e.ErrorCode)
                {
                    case StorageErrorCode.ContainerNotFound:
                        var message = string.Format(CultureInfo.InvariantCulture, "Storage container was not found: '{0}'.",
                            this.FullPath);
                        throw new ViewContainerNotFoundException(message, e);
                    default:
                        throw;
                }
            }
        }

        public IEnumerable<ViewItemDetail> ListAllNestedItemsWithDetail()
        {
            try
            {
                return _directory.ListBlobs(new BlobRequestOptions())
                    .OfType<CloudBlob>()
                    .Select(item => new ViewItemDetail()
                    {
                        ItemName = _directory.Uri.MakeRelativeUri(item.Uri).ToString(),
                        LastModifiedUtc = item.Properties.LastModifiedUtc,
                        Length = item.Properties.Length
                    })
                    .ToArray();
            }
            catch (StorageClientException e)
            {
                switch (e.ErrorCode)
                {
                    case StorageErrorCode.ContainerNotFound:
                        var message = string.Format(CultureInfo.InvariantCulture, "Storage container was not found: '{0}'.",
                            this.FullPath);
                        throw new ViewContainerNotFoundException(message, e);
                    default:
                        throw;
                }
            }
        }

        public bool ContainerExists()
        {
            try
            {
                _directory.Container.FetchAttributes();
                return true;
            }
            catch (StorageClientException e)
            {
                switch (e.ErrorCode)
                {
                    case StorageErrorCode.ContainerNotFound:
                    case StorageErrorCode.ResourceNotFound:
                    case StorageErrorCode.BlobNotFound:
                        return false;
                    default:
                        throw;
                }
            }
        }

        public string FullPath
        {
            get { return _directory.Uri.ToString(); }
        }
    }

}