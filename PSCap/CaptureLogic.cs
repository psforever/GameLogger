using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCap
{
    enum AttachState
    {
        Detached,
        Attached
    }

    enum CaptureState
    {
        NotCapturing,
        Capturing
    }

    

    class CaptureLogic
    {
        AttachState attachState = AttachState.Detached;
        CaptureState captureState = CaptureState.NotCapturing;

        ProcessCollectable currentProcess;

        public event EventHandler AttachedProcessExited;

        public bool isCapturing()
        {
            return captureState == CaptureState.Capturing;
        }

        public bool isAttached()
        {
            return attachState == AttachState.Attached;
        }

        public ProcessCollectable getAttachedProcess()
        {
            Debug.Assert(attachState == AttachState.Attached, "Tried to get process when not attached");

            return currentProcess;
        }

        public void detach()
        {
            Debug.Assert(attachState == AttachState.Attached, "Tried to detach twice");

            Log.Info("detaching from process {0}", currentProcess);

            if(captureState == CaptureState.Capturing)
            {
                Log.Info("currently capturing...stopping capture first");
                stopCapture();
            }
            else
            {
                Log.Info("wasn't capturing...going ahead with detach");
            }

            attachState = AttachState.Detached;
        }

        public void attach(ProcessCollectable process)
        {
            Debug.Assert(attachState == AttachState.Detached, "Must be detached before attaching");

            Log.Info("attaching to process {0}", process);

            // begin listening for events related to this process
            Process p = process.Process;

            p.EnableRaisingEvents = true;
            p.Exited += new EventHandler(AttachedProcessExited);

            currentProcess = process;
            attachState = AttachState.Attached;
        }

        public void capture()
        {
            Debug.Assert(captureState == CaptureState.NotCapturing &&
                attachState == AttachState.Attached, "Must be attached before capturing");

            Log.Info("capturing data from process {0}", currentProcess);

            captureState = CaptureState.Capturing;
        }

        public void stopCapture()
        {
            Debug.Assert(captureState == CaptureState.Capturing &&
                attachState == AttachState.Attached, "Must be attached before capturing");

            Log.Info("stopping data capture from process {0}", currentProcess);

            captureState = CaptureState.NotCapturing;
        }
    }
}
