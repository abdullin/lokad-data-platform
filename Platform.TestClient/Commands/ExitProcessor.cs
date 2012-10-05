namespace Platform.TestClient.Commands
{
    public class ExitProcessor : ICommandProcessor
    {
        public string Key { get { return "EXIT"; } }
        public string Usage { get { return Key; } }
        public bool Execute(CommandProcessorContext context, string[] args)
        {
            context.Log.Info("Exiting...");
            Application.Exit(ExitCode.Success, "Exit processor called");
            return true;
        }
    }
}