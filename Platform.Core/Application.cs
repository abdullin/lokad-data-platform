using System;

namespace Platform
{
    public class Application
    {
        private static readonly ILogger Log = LogManager.GetLoggerFor<Application>();
        private static Action<int> _exit;
        private static bool _initialized;

        public static void Start(Action<int> exitAction)
        {
            if (_initialized)
                throw new InvalidOperationException("Application is already initialized");

            _exit = exitAction;
            _initialized = true;
        }

        public static void Exit(ExitCode exitCode, string reason)
        {
            if (!_initialized)
                throw new InvalidOperationException("Application should be initialized before exiting");
            //Ensure.NotNullOrEmpty(reason, "reason");

            Log.Info("Exiting...");
            Log.Info("Exit reason : {0}", reason);

            LogManager.Finish();
            _exit((int)exitCode);
        }
    }

    public enum ExitCode
    {
        Success = 0,
        Error
    }
}