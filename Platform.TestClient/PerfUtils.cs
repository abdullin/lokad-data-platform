namespace Platform.TestClient
{
    public static class PerfUtils
    {
        private static readonly ILogger Log = LogManager.GetLoggerFor<Client>();

        /// <summary>
        /// Helps to print out team-city performance statistics in format
        /// <code><![CDATA[##teamcity[buildStatisticValue key='<valueTypeKey>' value='<value>']]]></code>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void LogTeamCityGraphData(string key, long value)
        {
            if (value < 0)
            {
                Log.Error("value is {0}, however TeamCity requires Value as non negative integer.", value);
                return;
            }

            
            const string teamCityFormat = "##teamcity[buildStatisticValue key='{0}' value='{1}']";
            Log.Debug(teamCityFormat, key, value);
        }
    }
}
