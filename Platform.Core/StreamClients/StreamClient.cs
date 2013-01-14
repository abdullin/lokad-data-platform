using System;
using System.Collections.Generic;
using Platform.StreamStorage;

namespace Platform.StreamClients
{
    public sealed class StreamClient
    {
        public readonly IRawStreamClient Advanced;
        readonly Func<Queue<Exception>, bool> _actionPolicy;

        public StreamClient(IRawStreamClient advanced, Func<Queue<Exception>, bool> policy)
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
}