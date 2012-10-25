using System;
using NLog;

namespace Platform
{
    /// <summary>
    /// Static class that is responsible for wiring in
    /// proper logger infrastructure (NLOG in this case)
    /// </summary>
    public static class LogManager
    {
        private static bool _initialized;

        public static string LogsDirectory
        {
            get
            {
                EnsureInitialized();
                return Environment.GetEnvironmentVariable("EVENTSTORE_LOGSDIR");
            }
        }

        public static ILogger GetLoggerFor<T>()
        {
            return GetLogger(typeof(T).Name);
        }

        public static ILogger GetLogger(string logName)
        {
            return new LazyLogger(() => new NLogger(logName));
        }

        public static void Init(string componentName, string logsDirectory)
        {
            //Ensure.NotNull(componentName, "componentName");
            if (_initialized)
                throw new InvalidOperationException("Cannot initialize twice");

            _initialized = true;

            SetLogsDirectoryIfNeeded(logsDirectory);
            SetComponentName(componentName);
            RegisterGlobalExceptionHandler();
        }

        public static void Finish()
        {
            NLog.LogManager.Configuration = null;
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("Init method must be called");
        }

        private static void SetLogsDirectoryIfNeeded(string logsDirectory)
        {
            const string logsDirEnvVar = "EVENTSTORE_LOGSDIR";
            var directory = Environment.GetEnvironmentVariable(logsDirEnvVar);
            if (directory == null)
            {
                directory = logsDirectory;
                Environment.SetEnvironmentVariable(logsDirEnvVar, directory, EnvironmentVariableTarget.Process);
            }
        }

        private static void SetComponentName(string componentName)
        {
            Environment.SetEnvironmentVariable("EVENTSTORE_INT-COMPONENT-NAME", componentName, EnvironmentVariableTarget.Process);
        }

        private static void RegisterGlobalExceptionHandler()
        {
            var globalLogger = GetLogger("GLOBAL-LOGGER");
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var exc = e.ExceptionObject as Exception;
                if (exc != null)
                {
                    globalLogger.FatalException(exc, "Global Unhandled Exception occured.");
                }
                else
                {
                    globalLogger.Fatal("Global Unhandled Exception object: {0}.", e.ExceptionObject);
                }
            };
        }
    }

    public interface ILogger
    {
        void Fatal(string text);

        void Error(string text);

        void Info(string text);

        void Debug(string text);

        void Trace(string text);

        void Fatal(string format, params object[] args);

        void Error(string format, params object[] args);

        void Info(string format, params object[] args);

        void Debug(string format, params object[] args);

        void Trace(string format, params object[] args);

        void FatalException(Exception exc, string text);

        void ErrorException(Exception exc, string text);

        void InfoException(Exception exc, string text);

        void DebugException(Exception exc, string text);

        void TraceException(Exception exc, string text);

        void FatalException(Exception exc, string format, params object[] args);

        void ErrorException(Exception exc, string format, params object[] args);

        void InfoException(Exception exc, string format, params object[] args);

        void DebugException(Exception exc, string format, params object[] args);

        void TraceException(Exception exc, string format, params object[] args);
    }
    
    class LazyLogger : ILogger
    {
        private readonly Lazy<ILogger> _logger;

        public LazyLogger(Func<ILogger> factory)
        {
            //Ensure.NotNull(factory, "factory");
            _logger = new Lazy<ILogger>(factory);
        }

        public void Fatal(string text)
        {
            _logger.Value.Fatal(text);
        }

        public void Error(string text)
        {
            _logger.Value.Error(text);
        }

        public void Info(string text)
        {
            _logger.Value.Info(text);
        }

        public void Debug(string text)
        {
            _logger.Value.Debug(text);
        }

        public void Trace(string text)
        {
            _logger.Value.Trace(text);
        }

        public void Fatal(string format, params object[] args)
        {
            _logger.Value.Fatal(format, args);
        }

        public void Error(string format, params object[] args)
        {
            _logger.Value.Error(format, args);
        }

        public void Info(string format, params object[] args)
        {
            _logger.Value.Info(format, args);
        }

        public void Debug(string format, params object[] args)
        {
            _logger.Value.Debug(format, args);
        }

        public void Trace(string format, params object[] args)
        {
            _logger.Value.Trace(format, args);
        }

        public void FatalException(Exception exc, string format)
        {
            _logger.Value.FatalException(exc, format);
        }

        public void ErrorException(Exception exc, string format)
        {
            _logger.Value.ErrorException(exc, format);
        }

        public void InfoException(Exception exc, string format)
        {
            _logger.Value.InfoException(exc, format);
        }

        public void DebugException(Exception exc, string format)
        {
            _logger.Value.DebugException(exc, format);
        }

        public void TraceException(Exception exc, string format)
        {
            _logger.Value.TraceException(exc, format);
        }

        public void FatalException(Exception exc, string format, params object[] args)
        {
            _logger.Value.FatalException(exc, format, args);
        }

        public void ErrorException(Exception exc, string format, params object[] args)
        {
            _logger.Value.ErrorException(exc, format, args);
        }

        public void InfoException(Exception exc, string format, params object[] args)
        {
            _logger.Value.InfoException(exc, format, args);
        }

        public void DebugException(Exception exc, string format, params object[] args)
        {
            _logger.Value.DebugException(exc, format, args);
        }

        public void TraceException(Exception exc, string format, params object[] args)
        {
            _logger.Value.TraceException(exc, format, args);
        }
    }

    public class NLogger : ILogger
    {
        private readonly Logger _logger;

        public NLogger(string name)
        {
            _logger = NLog.LogManager.GetLogger(name);
        }

        public void Fatal(string text)
        {
            _logger.Fatal(text);
        }

        public void Error(string text)
        {
            _logger.Error(text);
        }

        public void Info(string text)
        {
            _logger.Info(text);
        }

        public void Debug(string text)
        {
            _logger.Debug(text);
        }

        public void Trace(string text)
        {
            _logger.Trace(text);
        }


        public void Fatal(string format, params object[] args)
        {
            _logger.Fatal(format, args);
        }

        public void Error(string format, params object[] args)
        {
            _logger.Error(format, args);
        }

        public void Info(string format, params object[] args)
        {
            _logger.Info(format, args);
        }

        public void Debug(string format, params object[] args)
        {
            _logger.Debug(format, args);
        }

        public void Trace(string format, params object[] args)
        {
            _logger.Trace(format, args);
        }


        public void FatalException(Exception exc, string format)
        {
            _logger.FatalException(format, exc);
        }

        public void ErrorException(Exception exc, string format)
        {
            _logger.ErrorException(format, exc);
        }

        public void InfoException(Exception exc, string format)
        {
            _logger.InfoException(format, exc);
        }

        public void DebugException(Exception exc, string format)
        {
            _logger.DebugException(format, exc);
        }

        public void TraceException(Exception exc, string format)
        {
            _logger.TraceException(format, exc);
        }


        public void FatalException(Exception exc, string format, params object[] args)
        {
            _logger.FatalException(string.Format(format, args), exc);
        }

        public void ErrorException(Exception exc, string format, params object[] args)
        {
            _logger.ErrorException(string.Format(format, args), exc);
        }

        public void InfoException(Exception exc, string format, params object[] args)
        {
            _logger.InfoException(string.Format(format, args), exc);
        }

        public void DebugException(Exception exc, string format, params object[] args)
        {
            _logger.DebugException(string.Format(format, args), exc);
        }

        public void TraceException(Exception exc, string format, params object[] args)
        {
            _logger.TraceException(string.Format(format, args), exc);
        }
    }
}