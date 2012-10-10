namespace Platform.Storage
{
    public interface IAppendOnlyStreamReader
    {
        ReadResult ReadAll(long startOffset);
    }
}
