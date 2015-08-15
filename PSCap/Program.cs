using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSCap
{
    static class Program
    {
        static Mutex loggerMutex = null;
        static int loggerId = -1;
        const int MAX_LOGGERS = 5;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // before everything, make sure we can acquire a unique mutex
            if (!acquireNextMutex())
            {
                MessageBox.Show("Failed to aquire a logging ID. You are limited to " + MAX_LOGGERS + " PSLoggers.",
                    "Failed to Initialize",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(loggerId));
        }

        // used to establish a unique logger ID and therefore a unique pipe
        private static bool acquireNextMutex()
        {
            Mutex mutex = null;
            string mutexNameBase = "Global\\PSLoggerMutex";

            for (int i = 0; i < MAX_LOGGERS; i++)
            {
                string mutexName = mutexNameBase + i.ToString();
                bool createdNew;
                mutex = new Mutex(true, mutexName, out createdNew);

                if (createdNew)
                {
                    loggerMutex = mutex; // dont let it be disposed
                    loggerId = i;
                    return true;
                }

                if (mutex != null)
                    mutex.Close();
            }

            return false;
        }
    }
}
