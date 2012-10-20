using System.Threading;

namespace Platform.TestClient
{
    /// <summary>
    /// Single command processor of a test client
    /// </summary>
    public interface ICommandProcessor
    {
        /// <summary>
        /// Keyword to trigger this processor
        /// </summary>
        string Key { get; }
        /// <summary>
        /// Human-readable description
        /// </summary>
        string Usage { get; }
        /// <summary>
        /// Implement this to execute processor
        /// </summary>
        bool Execute(CommandProcessorContext context, CancellationToken token, string[] args);
    }
}