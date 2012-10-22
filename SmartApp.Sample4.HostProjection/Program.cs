using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartApp.Sample4.HostProjection
{
    class Program
    {
        static ConcurrentDictionary<string, DateTime> _lastChangedDate = new ConcurrentDictionary<string, DateTime>();
        private static List<Process> _startingProcess;
        static void Main(string[] args)
        {
            _startingProcess = new List<Process>();

            Task.Factory.StartNew(x => MonitoringProjectionSourcePath(), TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
            Console.ReadKey();
        }

        private static void MonitoringProjectionSourcePath()
        {
            Directory.CreateDirectory(@"C:\LokadData\dp-projection-store");
            var folderWatcher = new FileSystemWatcher();
            folderWatcher.Path = @"C:\LokadData\dp-projection-store";
            folderWatcher.IncludeSubdirectories = true;
            folderWatcher.Filter = "*.exe";
            folderWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            folderWatcher.Changed += FolderWatcher_Changed;
            folderWatcher.Created += FolderWatcher_Created;
            folderWatcher.Deleted += FolderWatcher_Deleted;
            folderWatcher.Renamed += FolderWatcher_Renamed;

            folderWatcher.EnableRaisingEvents = true;
        }

        static void FolderWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            StopProjectionExe(e.FullPath);
        }

        static void FolderWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            RestartProjection(e.FullPath, e.FullPath);
        }

        static void FolderWatcher_Created(object sender, FileSystemEventArgs e)
        {
            RestartProjection(e.FullPath, e.FullPath);
        }

        static void FolderWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            RestartProjection(e.OldFullPath, e.FullPath);
        }

        static void RestartProjection(string stopExePath, string startExePath)
        {
            if (_lastChangedDate.ContainsKey(stopExePath) && _lastChangedDate[stopExePath].AddSeconds(2) > DateTime.Now)
                return;

            StopProjectionExe(stopExePath);
            _lastChangedDate[stopExePath] = DateTime.Now;

            try
            {
                StartProjectionExe(startExePath);
            }
            catch (Exception exception)
            {
                Thread.Sleep(1000);
                try
                {
                    StartProjectionExe(startExePath);
                }
                catch (Exception)
                {

                }
            }
        }

        private static void StartProjectionExe(string startExePath)
        {
            Directory.CreateDirectory(@"C:\LokadData\dp-exe-store");
            var newPath = Path.Combine(@"C:\LokadData\dp-exe-store", Path.GetFileName(startExePath));

            if (File.Exists(newPath))
                File.Delete(newPath);
            File.Copy(startExePath, newPath);
            Process p = Process.Start(newPath);
            _startingProcess.Add(p);

            _lastChangedDate[startExePath] = DateTime.Now;
        }

        private static void StopProjectionExe(string stopExePath)
        {
            for (int i = 0; i < _startingProcess.Count; i++)
            {
                if (Path.GetFileName(_startingProcess[i].StartInfo.FileName).Equals(Path.GetFileName(stopExePath), StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!_startingProcess[i].HasExited)
                        _startingProcess[i].Kill();
                    _startingProcess.RemoveAt(i);
                    i--;
                    continue;
                }
            }
        }
    }
}
