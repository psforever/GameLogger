using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCap
{
    class CaptureFile
    {
        string captureName = "";
        bool modified = false;

        // start a blank capture file
        public CaptureFile()
        {
            captureName = string.Format("PSCap-{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now);
            modified = true;
        }

        public bool isModified()
        {
            return modified;
        }

        public override string ToString()
        {
            return captureName;
        }
    }
}
