#region (c) 2012 Lokad Data Platform - New BSD License 

// Copyright (c) Lokad 2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.IO;
using System.Threading;

namespace Platform.TestClient.Commands
{
    public class ResetStoreProcessor : ICommandProcessor
    {
        public string Key
        {
            get { return "RS"; }
        }

        public string Usage
        {
            get { return "RS folder"; }
        }

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            if (args.Length == 0)
            {
                context.Log.Error("Expected folder for test store");
                return false;
            }


            var dir = string.Join(" ", args);
            if (Directory.Exists(dir))
            {
                context.Log.Info("Cleaning {0}", dir);
                Directory.Delete(dir, true);
            }
            Directory.CreateDirectory(dir);
            const string variable = "DATAPLATFORM_STOREDIR";
            context.Log.Info("Setting environment variable: {0}", variable);
            
            Environment.SetEnvironmentVariable(variable, dir,EnvironmentVariableTarget.Machine);
            return true;
        }
    }
}