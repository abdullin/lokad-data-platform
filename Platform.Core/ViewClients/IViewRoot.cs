using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Platform.ViewClients
{
    /// <summary>
    /// Equivalent of streaming root from Lokad.CQRS. It abstracts away
    /// unrerlying binary storage (can be file, memory or Azure).
    /// </summary>
    public interface IViewRoot
    {
        /// <summary>
        /// Gets the container reference, identified by it's name
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>new container referece</returns>
        IViewContainer GetContainer(string name);

        /// <summary>
        /// Get a list of containers from the current root (supply non-null
        /// prefix to enable filtering)
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        IEnumerable<string> ListContainers(string prefix = null);
    }

    /// <summary>
    /// Represents storage container reference, equivalent of streaming
    /// container from Lokad.CQRS.
    /// </summary>
    public interface IViewContainer
    {
        /// <summary>
        /// Gets the full path.
        /// </summary>
        /// <value>The full path.</value>
        string FullPath { get; }

        /// <summary>
        /// Gets the child container nested within the current container reference.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        IViewContainer GetContainer(string name);

        /// <summary>
        /// Opens file in the current container for reading operation.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Stream OpenRead(string name);
        /// <summary>
        /// Open file in the current container for the writing operations.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Stream OpenWrite(string name);
        void TryDelete(string name);
        bool Exists(string name);

        //void AddOrUpdate(string name, Action<Stream> ifDoesNotExist, Action<Stream, Stream> ifExists);


        /// <summary>
        /// Ensures that the current reference represents valid container
        /// </summary>
        /// <returns></returns>
        IViewContainer Create();

        /// <summary>
        /// Deletes this container
        /// </summary>
        void Delete();

        /// <summary>
        /// Checks if the underlying container exists
        /// </summary>
        /// <returns></returns>
        bool Exists();

        IEnumerable<string> ListAllNestedItems();
        IEnumerable<ViewDetail> ListAllNestedItemsWithDetail();
    }

    public sealed class ViewDetail
    {
        public string Name;
        public DateTime LastModifiedUtc;
        public long Length;
    }

    [Serializable]
    public abstract class ViewException : Exception
    {
        protected ViewException(string message)
            : base(message)
        {
        }

        public ViewException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected ViewException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class ViewNotFoundException : ViewException
    {
        public ViewNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }


        protected ViewNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class ViewContainerNotFoundException : ViewException
    {
        public ViewContainerNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }


    }
}