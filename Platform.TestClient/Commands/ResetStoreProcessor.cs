#region (c) 2012 Lokad Data Platform - New BSD License 

// Copyright (c) Lokad 2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System.IO;
using System.Threading;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Platform.Storage.Azure;

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
            var location = context.Client.Options.StoreLocation;
            string dir = location;

            AzureStoreConfiguration configuration;
            if (AzureStoreConfiguration.TryParse(location, out configuration))
            {
                context.Log.Info("Azure store detected");
                var account = CloudStorageAccount.Parse(configuration.ConnectionString);
                var client = account.CreateCloudBlobClient();
                client.GetContainerReference(configuration.Container)
                    .ListBlobs()
                    .AsParallel().ForAll(i => client.GetBlobReference(i.Uri.ToString()).DeleteIfExists());
                return true;
            }

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