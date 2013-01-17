using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Platform.ViewClients
{
    /// <summary>
    /// Provides access to a list of containers located on on some 
    /// underlying binary storage (can be file, memory or Azure).
    /// </summary>
    public interface IRawViewRoot
    {
        /// <summary>
        /// Gets the container reference, identified by it's name.
        /// </summary>
        /// <param name="containerName">The name.</param>
        /// <returns>new container reference.</returns>
        IRawViewContainer GetContainer(string containerName);

        /// <summary>
        /// Get a list of containers from the current root (supply non-null
        /// prefix to enable filtering).
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        IEnumerable<string> ListContainers(string prefix = null);
    }

    /// <summary>
    /// Represents storage container reference with optional nested containers and binary large objects.
    /// It can correspond to either file system, cloud storage or in-memory, depending on the implementation.
    /// Use <see cref="ViewClient"/> if you need to operate views (it will handle transient exceptions)
    /// </summary>
    public interface IRawViewContainer
    {
        string FullPath { get; }

        /// <summary>
        /// Gets the child container nested within the current container reference.
        /// </summary>
        /// <param name="containerName">The name.</param>
        /// <returns></returns>
        IRawViewContainer GetContainer(string containerName);

        /// <summary>
        /// Opens file in the current container for reading operation..
        /// </summary>
        Stream OpenRead(string itemName);

        /// <summary>
        /// Open file in the current container for the writing operations.
        /// </summary>
        Stream OpenWrite(string itemName);

        void TryDeleteItem(string itemName);

        bool ItemExists(string itemName);

        //void AddOrUpdate(string name, Action<Stream> ifDoesNotExist, Action<Stream, Stream> ifExists);

        /// <summary>
        /// Ensures that the current reference represents valid container.
        /// </summary>
        IRawViewContainer EnsureContainerExists();

        /// <summary>
        /// Deletes this container.
        /// </summary>
        void DeleteContainer();

        /// <summary>
        /// Checks if the underlying container exists.
        /// </summary>
        bool ContainerExists();

        /// <summary>
        /// Lists relative names of all items within this container
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ViewContainerNotFoundException">If current container does not exist</exception>
        IEnumerable<string> ListAllNestedItems();
        /// <summary>Fetchs names, modification dates and sizes of all items within this container</summary>
        /// <exception cref="ViewContainerNotFoundException">If current container does not exist</exception>
        IEnumerable<ViewItemDetail> ListAllNestedItemsWithDetail();
    }

    public sealed class ViewItemDetail
    {
        /// <summary>
        /// Name of the item
        /// </summary>
        public string ItemName;
        /// <summary>
        /// Last modification date
        /// </summary>
        public DateTime LastModifiedUtc;
        /// <summary>
        /// Size in bytes
        /// </summary>
        public long Length;
    }

  

    [Serializable]
    public class ViewContainerNotFoundException : Exception
    {
        public ViewContainerNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected ViewContainerNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}