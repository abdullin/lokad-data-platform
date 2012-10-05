using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Platform.TestClient
{
    public class CommandProcessor
    {
        private readonly IDictionary<string, ICommandProcessor> _processors = new Dictionary<string, ICommandProcessor>();

        public void Register(ICommandProcessor commandProcessor)
        {
            var cmd = commandProcessor.Key.ToUpper();

            if (_processors.ContainsKey(cmd))
                throw new Exception(string.Format("The processor for command '{0}' is already registered", cmd));

            _processors.Add(cmd, commandProcessor);
        }

        public bool TryProcess(CommandProcessorContext context, string[] args)
        {
            if (args == null || args.Length == 0)
                throw new Exception("Empty command");

            var commandName = args[0].ToUpper();
            var commandArgs = args.Skip(1).ToArray();

            ICommandProcessor commandProcessor;
            if (!_processors.TryGetValue(commandName, out commandProcessor))
                return false;

            bool result = false;
            var executedEvent = new AutoResetEvent(false);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    result = commandProcessor.Execute(context, commandArgs);
                    executedEvent.Set();
                }
                catch (Exception exc)
                {
                    result = false;
                    //todo add log
                    executedEvent.Set();
                }
            });

            executedEvent.WaitOne(1000);
            context.WaitForCompletion();

            return result;
        }
    }
}