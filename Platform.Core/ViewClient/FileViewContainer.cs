using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Platform.ViewClient
{
    /// <summary>
    /// Storage container using <see cref="System.IO"/> for persisting data
    /// </summary>
    public sealed class FileViewContainer : IViewContainer, IViewRoot
    {
        readonly DirectoryInfo _root;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileViewContainer"/> class.
        /// </summary>
        /// <param name="root">The root.</param>
        public FileViewContainer(DirectoryInfo root)
        {
            _root = root;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileViewContainer"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        public FileViewContainer(string path)
            : this(new DirectoryInfo(path))
        {
        }

        public IViewContainer GetContainer(string name)
        {
            var child = new DirectoryInfo(Path.Combine(_root.FullName, name));
            return new FileViewContainer(child);
        }

        public IEnumerable<string> ListContainers(string prefix = null)
        {
            if (string.IsNullOrEmpty(prefix))
                return _root.GetDirectories().Select(d => d.Name);
            return _root.GetDirectories(prefix + "*").Select(d => d.Name);
        }

        public Stream OpenRead(string name)
        {
            var combine = Path.Combine(_root.FullName, name);

            // we allow concurrent reading
            // no more writers are allowed
            return File.Open(combine, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public Stream OpenWrite(string name)
        {
            var combine = Path.Combine(_root.FullName, name);

            // we allow concurrent reading
            // no more writers are allowed
            return File.Open(combine, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        }

        public void TryDelete(string name)
        {
            var combine = Path.Combine(_root.FullName, name);
            File.Delete(combine);
        }

        public bool Exists(string name)
        {
            return File.Exists(Path.Combine(_root.FullName, name));
        }


        public IViewContainer Create()
        {
            _root.Create();
            return this;
        }

        public void Delete()
        {
            _root.Refresh();
            if (_root.Exists)
                _root.Delete(true);
        }

        public bool Exists()
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

        public IEnumerable<ViewDetail> ListAllNestedItemsWithDetail()
        {
            try
            {
                return _root.GetFiles("*", SearchOption.AllDirectories).Select(f => new ViewDetail()
                    {
                        LastModifiedUtc = f.LastWriteTimeUtc,
                        Length = f.Length,
                        Name = f.Name
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