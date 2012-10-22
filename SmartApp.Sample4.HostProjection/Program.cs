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
        static Dictionary<string, DateTime> _lastChangedDate = new Dictionary<string, DateTime>();
        static Dictionary<string, DateTime> _lastStartTime = new Dictionary<string, DateTime>();
        private static List<Process> _startingProcess;
        static void Main(string[] args)
        {
            _startingProcess = new List<Process>();

            Task.Factory.StartNew(KillProcess);
            while (true)
            {
                MonitoringProjectionSourcePath();
                Thread.Sleep(5000);
                
            }
        }

        private static void KillProcess()
        {
            while (true)
            {
                var stopedProcess = new List<string>();
                foreach (KeyValuePair<string, DateTime> pair in _lastStartTime)
                {
                    if (pair.Value.AddSeconds(20) < DateTime.Now)
                        if(StopProjectionExe(pair.Key))stopedProcess.Add(pair.Key);
                }

                foreach (string s in stopedProcess)
                {
                    _lastStartTime.Remove(s);
                }

                Thread.Sleep(2000);
            }
        }

        private static void MonitoringProjectionSourcePath()
        {
            const string path = @"C:\LokadData\dp-projection-store";
            Directory.CreateDirectory(path);

            var deletedFile = _lastChangedDate.Select(x => x.Key).ToDictionary(x => x, y => File.Exists(y));

            foreach (KeyValuePair<string, bool> pair in deletedFile)
            {
                if(!pair.Value)
                {
                    StopProjectionExe(pair.Key);
                    _lastChangedDate.Remove(pair.Key);
                }
            }

            foreach (string file in Directory.GetFiles(path))
            {
                if (!_lastChangedDate.ContainsKey(file) || _lastChangedDate[file] != new FileInfo(file).LastWriteTime)
                {
                    _lastChangedDate[file] = new FileInfo(file).LastWriteTime;
                    RestartProjection(file, file);
                }
            }
        }

        static void RestartProjection(string stopExePath, string startExePath)
        {
            StopProjectionExe(stopExePath);

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

            StreamWriter streamWriter = null;

            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo(@"C:\Projects\lokad-data\SmartApp.Sample4.Test1\bin\Debug\SmartApp.Sample4.Test1.exe");
                processStartInfo.ErrorDialog = false;
                processStartInfo.UseShellExecute = false;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.RedirectStandardInput = true;
                processStartInfo.RedirectStandardOutput = true;

                //Execute the process
                Process process = new Process();
                _startingProcess.Add(process);
                process.StartInfo = processStartInfo;
                process.OutputDataReceived += process_OutputDataReceived;
                bool processStarted = process.Start();
                _lastStartTime[startExePath] = DateTime.Now;
                streamWriter = process.StandardInput;
                process.BeginOutputReadLine();
                if (processStarted)
                {
                    streamWriter.WriteLine("Message");
                    streamWriter.WriteLine("Message1");
                    streamWriter.WriteLine("Message2");
                    Thread.Sleep(10000);
                    streamWriter.WriteLine("Message3");
                    
                    process.WaitForExit();
                }

                //var pi = new ProcessStartInfo(newPath);
                //pi.UseShellExecute = false;
                //pi.RedirectStandardInput = true;
                //var p = Process.Start(pi);
                ////var p = new Process { StartInfo = { FileName = newPath ,UseShellExecute=false,CreateNoWindow=false} };
                //p.StartInfo.RedirectStandardInput = true;
                //p.Start();

                //p.StandardInput.WriteLine("This is all data");
                //bool success = false;
                //if (p.WaitForExit(35*1000*60))
                //{
                //    success = true;
                //}
                //else
                //{
                //    p.Kill();
                //}


                //

                //Console.WriteLine(p.StandardOutput.ReadToEnd());
            }
            catch (Exception exception)
            {
            }


        }

        static void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(e.Data))
            {
                Console.WriteLine(e.Data);
            }
        }

        private static bool StopProjectionExe(string stopExePath)
        {
            for (int i = 0; i < _startingProcess.Count; i++)
            {
                if (Path.GetFileName(_startingProcess[i].StartInfo.FileName).Equals(Path.GetFileName(stopExePath), StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!_startingProcess[i].HasExited)
                        _startingProcess[i].Kill();
                    _startingProcess.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
    }
}
