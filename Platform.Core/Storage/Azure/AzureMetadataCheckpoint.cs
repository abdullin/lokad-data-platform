using System;
using System.Globalization;
using Microsoft.WindowsAzure.StorageClient;

namespace Platform.Storage.Azure
{
    public class AzureMetadataCheckpoint
    {
        readonly CloudPageBlob _blob;
        static readonly ILogger Log = LogManager.GetLoggerFor<AzureMetadataCheckpoint>();
        readonly bool _readOnly;

        AzureMetadataCheckpoint(CloudPageBlob blob, bool readOnly)
        {
            _readOnly = readOnly;
            _blob = blob;
            Log.Debug("Checkpoint created");
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
            Log.Debug("Checkpoint were '{0}'", s ?? "N/A");
            var read = Int64.Parse(s ?? "0");
            return read;
        }

        public static AzureMetadataCheckpoint OpenWriteable(CloudPageBlob blob)
        {
            return new AzureMetadataCheckpoint(blob, readOnly:false);
        }

        public static AzureMetadataCheckpoint OpenReadable(CloudPageBlob blob)
        {
            return new AzureMetadataCheckpoint(blob, readOnly:true);
        }
    }
}