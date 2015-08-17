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
        Process process;

        public ProcessCollectable(Process p)
        {
            process = p;
        }

        public Process Process
        {
            get { return process; }
        }

        public override string ToString()
        {
            return process.ProcessName + " (PID " + process.Id + ")";
        }
    }
}
