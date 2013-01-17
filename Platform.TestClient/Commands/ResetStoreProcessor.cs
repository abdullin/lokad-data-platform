#region (c) 2012 Lokad Data Platform - New BSD License 

// Copyright (c) Lokad 2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Threading;
using ServiceStack.ServiceClient.Web;

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
            get { return "RS"; }
        }

        public bool Execute(CommandProcessorContext context, CancellationToken token, string[] args)
        {
            try
            {
                var result = new JsonServiceClient(context.Client.ClientHttpBase)
                    .Post<ClientApi.ResetStoreResponse>(ClientApi.ResetStore.Url, new ClientApi.ResetStore());

                return result.Success;
            }
            catch (Exception e)
            {
                context.Log.Info("Failed to get response: " + e.Message);
                return false;
            }
        }
    }
}