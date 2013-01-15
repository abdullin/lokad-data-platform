using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Platform.ViewClients
{
    /// <summary>
    /// Equivalent of streaming root from Lokad.CQRS. It abstracts away
    /// underlying binary storage (can be file, memory or Azure).
    /// </summary>
    public interface IRawViewRoot
    {
        /// <summary>
        /// Gets the container reference, identified by it's name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>new container reference.</returns>
        IRawViewContainer GetContainer(string name);

        /// <summary>
        /// Get a list of containers from the current root (supply non-null
        /// prefix to enable filtering).
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        IEnumerable<string> ListContainers(string prefix = null);
    }

    /// <summary>
    /// Represents storage container reference, equivalent of streaming
    /// container from Lokad.CQRS. Use <see cref="ViewClient"/> if you need
    /// to operate views (it will handle exceptions)
    /// </summary>
    public interface IRawViewContainer
    {
        string FullPath { get; }

        /// <summary>
        /// Gets the child container nested within the current container reference.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        IRawViewContainer GetContainer(string name);

        /// <summary>
        /// Opens file in the current container for reading operation..
        /// </summary>
        Stream OpenRead(string name);

        /// <summary>
        /// Open file in the current container for the writing operations.
        /// </summary>
        Stream OpenWrite(string name);

        void TryDelete(string name);

        bool Exists(string name);

        //void AddOrUpdate(string name, Action<Stream> ifDoesNotExist, Action<Stream, Stream> ifExists);

        /// <summary>
        /// Ensures that the current reference represents valid container.
        /// </summary>
        IRawViewContainer Create();

        /// <summary>
        /// Deletes this container.
        /// </summary>
        void Delete();

        /// <summary>
        /// Checks if the underlying container exists.
        /// </summary>
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