using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Platform.TestClient
{
    public static class PerfUtils
    {
        private static readonly ILogger Log = LogManager.GetLoggerFor<Client>();

        public static void LogTeamCityGraphData(string key, long value)
        {
            if (value < 0)
            {
                Log.Error("value is {0}, however TeamCity requires Value as a positive (non negative) integer.", value);
                return;
            }

            // ##teamcity[buildStatisticValue key='<valueTypeKey>' value='<value>']
            const string teamCityFormat = "##teamcity[buildStatisticValue key='{0}' value='{1}']";
            Console.WriteLine(teamCityFormat, key, value);
            //Log.Debug(teamCityFormat, key, value);
        }
    }
}
