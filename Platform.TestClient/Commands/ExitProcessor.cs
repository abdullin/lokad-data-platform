namespace Platform.TestClient.Commands
{
    public class ExitProcessor:ICommandProcessor
    {
        public string Key { get { return "EXIT"; } }
        public bool Execute(CommandProcessorContext context, string[] args)
        {
            Application.Exit(ExitCode.Success, "Exit processor called");
            return true;
        }
    }
}