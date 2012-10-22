using System.Collections.Generic;
using System.IO;

namespace Platform
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
}