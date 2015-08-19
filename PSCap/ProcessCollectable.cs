using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCap
{
    class ProcessCollectable
    {
        public Process Process { get; }

        public ProcessCollectable(Process p)
        {
            Process = p;
        }

        public override string ToString()
        {
            return Process.ProcessName + " (PID " + Process.Id + ")";
        }
    }
}
