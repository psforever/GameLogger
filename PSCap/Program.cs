using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSCap
{
    static class Program
    {
        static Mutex loggerMutex = null;
        public static int LoggerId { get; set; }
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

            setupErrorHandling();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PSCapMain(LoggerId));
        }

        private static void setupErrorHandling()
        {
            Application.ThreadException += new ThreadExceptionEventHandler(CatchUnhandledException);

            Trace.Listeners.Clear();
            TraceListener listener = new GameLoggerTraceListener();
            listener.TraceOutputOptions |= TraceOptions.Callstack | TraceOptions.DateTime;

            Trace.Listeners.Add(listener);
        }

        // taken from https://stackoverflow.com/questions/5710148/c-sharp-unhandled-exception-handler-attempting-to-write-to-log-file
        private static void CatchUnhandledException(object sender, ThreadExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            Trace.Fail("Unhandled exception: " + ex.Message, ex.StackTrace);
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
                    LoggerId = i;
                    return true;
                }

                if (mutex != null)
                    mutex.Close();
            }

            LoggerId = -1;

            return false;
        }
    }

    class GameLoggerTraceListener : TraceListener
    {
        public override void Fail(string message)
        {
            Fail(message, "");
        }

        public override void Fail(string message, string exMessage)
        {
            Log.Fatal("Fatal error has occurred");
            WriteLine("Error: \"" + message + "\"");

            if(exMessage != "")
                WriteLine(exMessage);

            WriteLine(Environment.StackTrace);

            MessageBox.Show("Unhandled error: " + message + "\nPlease submit log file for review",
                "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            Application.Exit();
        }

        public override void Write(object o)
        {
            Write(o.ToString());
        }

        public override void WriteLine(object o)
        {
            WriteLine(o.ToString());
        }

        public override void Write(string w)
        {
            Log.Raw(w);
        }

        public override void WriteLine(string w)
        {
            Write(w + Environment.NewLine);
        }

    }
}
