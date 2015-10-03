using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCap
{
    public static class Log
    {
        public static StreamWriter logFile = null;

        public static void Error(string fmt, params object[] args)
        {
            DoLog("error", fmt, args);
        }

        public static void Info(string fmt, params object[] args)
        {
            DoLog("info", fmt, args);
        }

        public static void Debug(string fmt, params object[] args)
        {
            DoLog("debug", fmt, args);
        }

        public static void Warning(string fmt, params object[] args)
        {
            DoLog("warning", fmt, args);
        }

        public static void Fatal(string fmt, params object[] args)
        {
            DoLog("fatal", fmt, args);
        }

        private static void DoLog(string prefix, string fmt, params object[] args)
        {
            Raw(string.Format("[" + prefix + "] " + fmt, args) + Environment.NewLine);
        }

        public static void Raw(string data)
        {
            if(logFile != null)
            {
                logFile.Write(data);
            }

            Console.Write(data);
        }
    }
}
