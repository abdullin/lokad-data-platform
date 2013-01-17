using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Platform.ViewClients
{
    /// <summary>
    /// Storage container using <see cref="System.IO"/> for persisting data
    /// </summary>
    public sealed class FileViewRoot : IRawViewContainer, IRawViewRoot
    {
        readonly DirectoryInfo _root;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileViewRoot"/> class.
        /// </summary>
        /// <param name="root">The root.</param>
        public FileViewRoot(DirectoryInfo root)
        {
            _root = root;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileViewRoot"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        public FileViewRoot(string path)
            : this(new DirectoryInfo(path))
        {
        }

        public IRawViewContainer GetContainer(string containerName)
        {
            var child = new DirectoryInfo(Path.Combine(_root.FullName, containerName));
            return new FileViewRoot(child);
        }

        public IEnumerable<string> ListContainers(string prefix = null)
        {
            if (string.IsNullOrEmpty(prefix))
                return _root.GetDirectories().Select(d => d.Name);
            return _root.GetDirectories(prefix + "*").Select(d => d.Name);
        }

        public Stream OpenRead(string itemName)
        {
            var combine = Path.Combine(_root.FullName, itemName);

            // we allow concurrent reading
            // no more writers are allowed
            return File.Open(combine, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public Stream OpenWrite(string itemName)
        {
            var combine = Path.Combine(_root.FullName, itemName);

            // we allow concurrent reading
            // no more writers are allowed
            return File.Open(combine, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        }

        public void TryDeleteItem(string itemName)
        {
            var combine = Path.Combine(_root.FullName, itemName);
            File.Delete(combine);
        }

        public bool ItemExists(string itemName)
        {
            return File.Exists(Path.Combine(_root.FullName, itemName));
        }


        public IRawViewContainer EnsureContainerExists()
        {
            _root.Create();
            return this;
        }

        public void DeleteContainer()
        {
            _root.Refresh();
            if (_root.Exists)
                _root.Delete(true);
        }

        public bool ContainerExists()
        {
            _root.Refresh();
            return _root.Exists;
        }

        public IEnumerable<string> ListAllNestedItems()
        {
            try
            {
                return _root.GetFiles().Select(f => f.Name).ToArray();
            }
            catch (DirectoryNotFoundException e)
            {
                var message = string.Format(CultureInfo.InvariantCulture, "Storage container was not found: '{0}'.",
                    this.FullPath);
                throw new ViewContainerNotFoundException(message, e);
            }
        }

        public IEnumerable<ViewItemDetail> ListAllNestedItemsWithDetail()
        {
            try
            {
                return _root.GetFiles("*", SearchOption.AllDirectories).Select(f => new ViewItemDetail()
                    {
                        LastModifiedUtc = f.LastWriteTimeUtc,
                        Length = f.Length,
                        ItemName = f.Name
                    }).ToArray();
            }
            catch (DirectoryNotFoundException e)
            {
                var message = string.Format(CultureInfo.InvariantCulture, "Storage container was not found: '{0}'.",
                    this.FullPath);
                throw new ViewContainerNotFoundException(message, e);
            }
        }

        public string FullPath
        {
            get { return _root.FullName; }
        }
    }
}