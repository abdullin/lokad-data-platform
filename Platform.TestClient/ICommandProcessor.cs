namespace Platform.TestClient
{
    public interface ICommandProcessor
    {
        string Key { get; }
        string Usage { get; }
        bool Execute(CommandProcessorContext context, string[] args);
    }
}