using System.Collections.Generic;

namespace Platform.Storage
{
    public interface IAppendOnlyStreamReader
    {
        IEnumerable<DataRecord> ReadAll(long startOffset, int maxRecordCount = int.MaxValue);
    }
}
