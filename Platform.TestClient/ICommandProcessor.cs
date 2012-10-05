namespace Platform.TestClient
{
    public interface ICommandProcessor
    {
        string Key { get; }
        bool Execute(CommandProcessorContext context, string[] args);
    }
}