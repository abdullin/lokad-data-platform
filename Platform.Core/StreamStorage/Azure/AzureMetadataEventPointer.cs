using System;
using System.Globalization;
using Microsoft.WindowsAzure.StorageClient;

namespace Platform.StreamStorage.Azure
{
    /// <summary>
    /// Maintains a pointer to a specific event from event store in
    /// a metadata of azure cloud page blob
    /// </summary>
    public class AzureMetadataEventPointer
    {
        readonly CloudPageBlob _blob;
        static readonly ILogger Log = LogManager.GetLoggerFor<AzureMetadataEventPointer>();
        readonly bool _readOnly;

        AzureMetadataEventPointer(CloudPageBlob blob, bool readOnly)
        {
            _readOnly = readOnly;
            _blob = blob;
        }

        public void Write(long checkpoint)
        {
            if (_readOnly)
                throw new NotSupportedException("This checkpoint is not writeable.");
            Log.Debug("Set checkpoint to {0}", checkpoint);
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

        public static AzureMetadataEventPointer OpenWriteable(CloudPageBlob blob)
        {
            return new AzureMetadataEventPointer(blob, readOnly:false);
        }

        public static AzureMetadataEventPointer OpenReadable(CloudPageBlob blob)
        {
            return new AzureMetadataEventPointer(blob, readOnly:true);
        }

        public void Dispose()
        {
            
        }
    }
}