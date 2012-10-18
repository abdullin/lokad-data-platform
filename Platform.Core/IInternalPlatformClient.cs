using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Platform.Storage;

namespace Platform
{
    /// <summary>
    /// Provides raw byte-level access to the storage and messaging of
    /// Data platform
    /// </summary>
    public interface IInternalPlatformClient
    {
        /// <summary>
        /// Returns lazy enumeration over all events in a given record range. 
        /// </summary>
        IEnumerable<RetrievedDataRecord> ReadAll(StorageOffset startOffset = default (StorageOffset), int maxRecordCount = int.MaxValue);
        void WriteEvent(string streamName, byte[] data);
        void WriteEventsInLargeBatch(string streamName, IEnumerable<RecordForStaging> records);
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct StorageOffset
    {
        public readonly long OffsetInBytes;

        public static readonly StorageOffset Zero = new StorageOffset(0);

        public override string ToString()
        {
            return string.Format("Offset {0}b", OffsetInBytes);
        }

        public StorageOffset(long offsetInBytes)
        {
            Ensure.Nonnegative(offsetInBytes, "offsetInBytes");
            OffsetInBytes = offsetInBytes;
        }

        public static   bool operator >(StorageOffset x , StorageOffset y)
        {
            return x.OffsetInBytes > y.OffsetInBytes;
        }
        public static bool operator <(StorageOffset x , StorageOffset y)
        {
            return x.OffsetInBytes < y.OffsetInBytes;
        }
        public static bool operator >= (StorageOffset left, StorageOffset right)
        {
            return left.OffsetInBytes >= right.OffsetInBytes;
        }
        public static bool operator <=(StorageOffset left, StorageOffset right)
        {
            return left.OffsetInBytes <= right.OffsetInBytes;
        }


    }

    public class FilePlatformClient : JsonPlatformClientBase, IInternalPlatformClient
    {
        readonly string _serverFolder;
        

        readonly string _checkStreamName;
        readonly string _fileStreamName;

        static readonly ILogger Log = LogManager.GetLoggerFor<FilePlatformClient>();

        
        public IEnumerable<RetrievedDataRecord> ReadAll(StorageOffset startOffset, int maxRecordCount)
        {
            if (maxRecordCount < 0)
                throw new ArgumentOutOfRangeException("maxRecordCount");

            var maxOffset = GetMaxOffset();

            // nothing to read from here
            if (startOffset >= maxOffset)
                yield break;


            if (!File.Exists(_fileStreamName))
                throw new InvalidOperationException("File stream.chk found but stream.dat file does not exist");

            using (var dataStream = new FileStream(_fileStreamName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var dataBits = new BitReader(dataStream))
            {
                var seekResult = dataStream.Seek(startOffset.OffsetInBytes, SeekOrigin.Begin);

                if (startOffset.OffsetInBytes != seekResult)
                    throw new InvalidOperationException("Failed to reach position we seeked for");

                int recordCount = 0;
                while (true)
                {
                    var key = dataBits.ReadString();
                    var length = dataBits.Reader7BitInt();
                    var data = dataBits.ReadBytes(length);

                    var currentOffset = new StorageOffset(dataStream.Position);
                    yield return new RetrievedDataRecord(key, data, currentOffset);

                    recordCount += 1;
                    if (recordCount >= maxRecordCount)
                        yield break;

                    if (currentOffset >= maxOffset)
                        yield break;
                }

            }
        }

        private StorageOffset GetMaxOffset()
        {

            if (!File.Exists(_checkStreamName))
                return StorageOffset.Zero;

            using (var checkStream = new FileStream(_checkStreamName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var checkBits = new BinaryReader(checkStream))
                {
                    return new StorageOffset(checkBits.ReadInt64());
                }
            }

        }

        sealed class BitReader : BinaryReader
        {
            public BitReader(Stream output) : base(output) { }

            public int Reader7BitInt()
            {
                return Read7BitEncodedInt();
            }
        }

        public FilePlatformClient(string serverFolder, string serverEndpoint = null) : base(serverEndpoint)
        {
            _serverFolder = serverFolder;
            
            var path = Path.GetFullPath(serverFolder ?? "");

            _checkStreamName = Path.Combine(path, "stream.chk");
            _fileStreamName = Path.Combine(path, "stream.dat");
        }

        public void WriteEventsInLargeBatch(string streamName, IEnumerable<RecordForStaging> records)
        {
            if (!Directory.Exists(_serverFolder))
                Directory.CreateDirectory(_serverFolder);

            var location = Path.Combine(_serverFolder, Guid.NewGuid().ToString());
            try
            {
                PrepareStaging(records, location);
                ImportEventsInternal(streamName, location);
            }
            finally
            {
                File.Delete(location);
            }
        }

        static void PrepareStaging(IEnumerable<RecordForStaging> records, string location)
        {
            using (var fs = File.OpenWrite(location))
            using (var bin = new BinaryWriter(fs))
            {
                foreach (var record in records)
                {
                    bin.Write(record.Data.Length);
                    bin.Write(record.Data);
                }
            }
        }
    }

    public struct RecordForStaging
    {
        public readonly byte[] Data;
        public RecordForStaging(byte[] data)
        {
            Data = data;
        }
    }
}
