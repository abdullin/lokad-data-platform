using System;
using System.Globalization;
using Microsoft.WindowsAzure.StorageClient;

namespace Platform.StreamStorage.Azure
{
    /// <summary>
    /// Not intended to be used outside <c>Platform.Core</c> itself.
    /// 
    /// Maintains a pointer to a specific event within the event store 
    /// using the metadata of a Windows Azure cloud page blob.
    /// </summary>
    public class AzureEventPointer : IEventPointer
    {
        readonly CloudPageBlob _blob;
        static readonly ILogger Log = LogManager.GetLoggerFor<AzureEventPointer>();
        readonly bool _readOnly;

        AzureEventPointer(CloudPageBlob blob, bool readOnly)
        {
            _readOnly = readOnly;
            _blob = blob;
        }

        public void Write(long checkpoint)
        {
            if (_readOnly)
                throw new NotSupportedException("This checkpoint is not writeable.");
            //Log.Debug("Set checkpoint to {0}", checkpoint);
            _blob.Metadata["committedsize"] = checkpoint.ToString(CultureInfo.InvariantCulture);
            _blob.SetMetadata();
        }

        public long Read()
        {
            _blob.FetchAttributes();
            var s = _blob.Metadata["committedsize"];
            //Log.Debug("Checkpoint were '{0}'", s ?? "N/A");
            var read = Int64.Parse(s ?? "0");
            return read;
        }

        public static AzureEventPointer OpenWriteable(CloudPageBlob blob)
        {
            return new AzureEventPointer(blob, readOnly:false);
        }

        public static AzureEventPointer OpenReadable(CloudPageBlob blob)
        {
            return new AzureEventPointer(blob, readOnly:true);
        }

        public void Dispose()
        {
            
        }
    }
}