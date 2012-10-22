using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Platform.ViewClient
{
    public interface IViewRoot
    {
        /// <summary>
        /// Gets the container reference, identified by it's name
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>new container referece</returns>
        IViewContainer GetContainer(string name);


        IEnumerable<string> ListContainers(string prefix = null);
    }

    /// <summary>
    /// Represents storage container reference.
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

        Stream OpenRead(string name);
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
    public class ViewException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public ViewException()
        {
        }

        public ViewException(string message)
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

        protected ViewContainerNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}