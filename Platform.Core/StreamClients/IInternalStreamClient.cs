using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using Platform.Storage;

namespace Platform.StreamClients
{
    [Serializable]
    public class PlatformClientException : Exception
    {
        public PlatformClientException(string message, Exception ex) : base(message, ex) {}
        public PlatformClientException(string message) : base(message) {}

    }


    /// <summary>
    /// Provides raw byte-level access to the storage and messaging of
    /// Data platform
    /// </summary>
    public interface IInternalStreamClient
    {
        /// <summary>
        /// Returns lazy enumeration over all events in a given record range. 
        /// </summary>
        IEnumerable<RetrievedDataRecord> ReadAll(StorageOffset startOffset = default (StorageOffset),
            int maxRecordCount = int.MaxValue);

        void WriteEvent(string streamName, byte[] data);
        void WriteEventsInLargeBatch(string streamKey, IEnumerable<RecordForStaging> records);
    }

    public sealed class StreamClient
    {
        public readonly IInternalStreamClient Advanced;
        readonly Func<Queue<Exception>, bool> _actionPolicy;

        public StreamClient(IInternalStreamClient advanced, Func<Queue<Exception>, bool> policy)
        {
            Advanced = advanced;
            _actionPolicy = policy;
        }

        public void WriteEvent(string streamName, byte[] data)
        {
            
        }

        

        public IEnumerable<RetrievedDataRecord> ReadAll(StorageOffset startOffset = default (StorageOffset),
            int maxRecordCount = int.MaxValue)
        {

            var position = startOffset;
            var remaining = maxRecordCount;

            Queue<Exception> errors = null;
            while (true)
            {
                if (remaining <= 0)
                    yield break;

                using (var enumerator = Advanced.ReadAll(position, remaining).GetEnumerator())
                {
                    try
                    {
                        if (!enumerator.MoveNext())
                            yield break;
                    }
                    catch (Exception ex)
                    {
                        if (errors == null)
                        {
                            errors = new Queue<Exception>();
                        }
                        errors.Enqueue(ex);
                        if (_actionPolicy(errors))
                            throw new PlatformClientException(ex.Message, new AggregateException(errors));
                    }

                    yield return enumerator.Current;
                    position = enumerator.Current.Next;
                    remaining -= 1;
                }
            }

            
        } 
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

    public struct RecordForStaging
    {
        public readonly byte[] Data;
        public RecordForStaging(byte[] data)
        {
            Data = data;
        }
    }


}
