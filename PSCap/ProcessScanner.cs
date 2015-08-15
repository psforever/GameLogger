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
        const int REFRESH_RATE = 2000;

        string targetProcessName = "";
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
            if (scanTaskObj != null)
                return;

            tokenSource = new CancellationTokenSource();
            scanTaskObj = Task.Factory.StartNew(() => scanTask(tokenSource.Token), tokenSource.Token);
        }

        public void stopScanning()
        {
            if(tokenSource != null)
            {
                tokenSource.Cancel();
                scanTaskObj.Wait();
                tokenSource = null;
            }
        }

        private void scanTask(CancellationToken ct)
        {
            int lastProcessesLength = -1;
            HashSet<int> curProcessSet = new HashSet<int>();

            while (!ct.IsCancellationRequested)
            {
                Process[] psProcesses = Process.GetProcessesByName(targetProcessName);

                if (psProcesses.Length != lastProcessesLength)
                {
                    lastProcessesLength = psProcesses.Length;
                }
                else
                {
                    // verify that all matched pids were still found, else update the list

                    Thread.Sleep(REFRESH_RATE);
                    continue;
                }

                ProcessListUpdate.Invoke(psProcesses);

                //Console.WriteLine("Got " + psProcesses.Length + " processes");

                if (psProcesses.Length == 0)
                {
  
                }
                else
                {

                    foreach (Process p in psProcesses)
                    {
                       
                    }
                }

                Thread.Sleep(REFRESH_RATE);
            }
        }

        public void scanOnce()
        {

        }

        /*public int getSelectedPID()
        {

        }

        public Process getSelectedProcess()
        {

        }*/
    }
}
