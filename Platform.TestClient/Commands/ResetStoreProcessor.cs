#region (c) 2012 Lokad Data Platform - New BSD License 

// Copyright (c) Lokad 2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System.IO;
using System.Threading;
using System.Linq;

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
            get { return "RS [dir]"; }
        }

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            string dir = context.Client.Options.StoreLocation;
            if (args.Any())
            {
                dir = string.Join(" ", args);
            }
            if (Directory.Exists(dir))
            {
                context.Log.Info("Cleaning {0}", dir);
                Directory.Delete(dir, true);
            }
            Directory.CreateDirectory(dir);
            
            return true;
        }
    }
}