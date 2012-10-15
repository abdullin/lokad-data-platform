using System.Collections.Generic;

namespace Platform.Storage
{
    public interface IPlatformClient
    {
        bool IsAzure { get; }
        IEnumerable<RetrievedDataRecord> ReadAll(long startOffset, int maxRecordCount = int.MaxValue);

        
    }
}
