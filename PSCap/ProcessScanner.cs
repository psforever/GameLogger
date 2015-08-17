using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PSCap
{
    delegate void ProcessListUpdateHandler(Process[] list);

    class ProcessScanner
    {
        const int REFRESH_RATE = 1000;

        string targetProcessName = "";
        bool taskStarted = false;
        Task scanTaskObj = null;
        CancellationTokenSource tokenSource = null;

        // call these on a new process list
        public event ProcessListUpdateHandler ProcessListUpdate;

        public ProcessScanner(string processName)
        {
            targetProcessName = processName;
        }

        public void startScanning()
        {
            if (taskStarted)
                return;

            taskStarted = true;
            tokenSource = new CancellationTokenSource();
            scanTaskObj = Task.Factory.StartNew(() => scanTask(tokenSource.Token), tokenSource.Token);
        }

        public void stopScanning()
        {
            if (!taskStarted)
                return;
            
            tokenSource.Cancel();
            taskStarted = false;
        }

        private void scanTask(CancellationToken ct)
        {
            Log.Debug("ProcessScanner started for " + targetProcessName);

            int lastProcessesLength = -1;
            HashSet<int> curProcessSet = new HashSet<int>();

            while (!ct.IsCancellationRequested)
            {
                Process[] psProcesses = Process.GetProcessesByName(targetProcessName);
                HashSet<int> newProcessSet = new HashSet<int>();

                foreach (Process p in psProcesses)
                    newProcessSet.Add(p.Id);

                if (psProcesses.Length != lastProcessesLength)
                {
                    lastProcessesLength = psProcesses.Length;
                }
                else
                {
                    // verify that all matched pids were still found, else update the list
                    if (curProcessSet.SetEquals(newProcessSet))
                    {
                        Thread.Sleep(REFRESH_RATE);
                        continue;
                    }
                }

                Log.Debug("ProcessScanner got new PID set");

                curProcessSet = newProcessSet;
                ProcessListUpdate.Invoke(psProcesses);

                Thread.Sleep(REFRESH_RATE);
            }

            Log.Debug("ProcessScanner stopped");
        }
    }
}
