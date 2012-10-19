using System;
using System.Configuration;
using System.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Platform.Node.Worker
{
    /// <summary>
    /// Settings provider built on top of the Windows Azure
    /// </summary>
    public sealed class AzureSettingsProvider
    {
        [DebuggerNonUserCode]
        static bool DetectCloudEnvironment()
        {
            try
            {
                if (RoleEnvironment.IsAvailable)
                    return true;
            }
            catch (RoleEnvironmentException)
            {
                // no environment
            }
            catch (Exception)
            {

            }
            return false;
        }

        static readonly Lazy<bool> HasCloudEnvironment = new Lazy<bool>(DetectCloudEnvironment, true);

        /// <summary>
        /// Attempts to get the configuration string from cloud environment or app settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="result">The result.</param>
        /// <returns><em>True</em> if configuration value is available, <em>False</em> otherwise</returns>
        public static bool TryGetString(string key, out string result)
        {
            result = TryGetString(key);
            return !string.IsNullOrEmpty(result);
        }

        public static string TryGetString(string key)
        {
            string result = null;
            if (HasCloudEnvironment.Value)
            {
                try
                {
                    result = RoleEnvironment.GetConfigurationSettingValue(key);
                }
                catch (RoleEnvironmentException)
                {
                    // no setting in dev?
                }
            }
            if (string.IsNullOrEmpty(result))
            {
                result = ConfigurationManager.AppSettings[key];
            }
            return result;
        }


        /// <summary>
        /// Attempts to get the configuration string from cloud environment or app settings. Throws the exception if not available.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>
        /// configuration value
        /// </returns>
        [DebuggerNonUserCode]
        public static string GetStringOrThrow(string key)
        {
            string result;
            if (!TryGetString(key, out result))
            {
                var s = string.Format("Failed to find configuration setting for '{0}'", key);
                throw new InvalidOperationException(s);
            }
            return result;
        }
    }
}
