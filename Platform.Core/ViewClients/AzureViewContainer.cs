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
    public sealed class AzureViewRoot : IViewRoot
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

        public IViewContainer GetContainer(string name)
        {
            return new AzureViewContainer(_client.GetBlobDirectoryReference(name));
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
    public sealed class AzureViewContainer : IViewContainer
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

        public IViewContainer GetContainer(string name)
        {
            if (name == null) throw new ArgumentNullException("name");

            return new AzureViewContainer(_directory.GetSubdirectory(name));
        }

        public Stream OpenRead(string name)
        {
            return _directory.GetBlobReference(name).OpenRead();
        }

        public Stream OpenWrite(string name)
        {
            return _directory.GetBlobReference(name).OpenWrite();
        }

        public void TryDelete(string name)
        {
            _directory.GetBlobReference(name).DeleteIfExists();
        }

        public bool Exists(string name)
        {
            try
            {
                _directory.GetBlobReference(name).FetchAttributes();
                return true;
            }
            catch (StorageClientException ex)
            {
                return false;
            }
        }


        public IViewContainer Create()
        {
            _directory.Container.CreateIfNotExist();
            return this;
        }

        /// <summary>
        /// Deletes this container
        /// </summary>
        public void Delete()
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

        public IEnumerable<ViewDetail> ListAllNestedItemsWithDetail()
        {
            try
            {
                return _directory.ListBlobs(new BlobRequestOptions())
                    .OfType<CloudBlob>()
                    .Select(item => new ViewDetail()
                    {
                        Name = _directory.Uri.MakeRelativeUri(item.Uri).ToString(),
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

        public bool Exists()
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