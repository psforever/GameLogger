using System;
using System.Diagnostics;
using System.IO;

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

    enum AttachResult
    {
        Success,
        DllNoProcess,
        DllInjectionFailure,
        DllMissing,
        DllConnection,
        PipeServerStartup,
        DllHandshake,
        UnknownFailure,
    }

    delegate void AttachResultCallback(bool okay, AttachResult result, string message);

    class CaptureLogic
    {
        AttachState attachState = AttachState.Detached;
        CaptureState captureState = CaptureState.NotCapturing;

        ProcessCollectable currentProcess;
        PipeServer pipeServer;
        string dllName;
        string pipeServerName;

        public event EventHandler AttachedProcessExited;

        public CaptureLogic(string pipeServerName, string dllName)
        {
            this.pipeServerName = pipeServerName;
            this.dllName = dllName;

            pipeServer = new PipeServer(pipeServerName);
        }

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

            pipeServer.stop();
            currentProcess.Process.Close();
            //currentProcess.Process.Dispose();
            //currentProcess = null;

            attachState = AttachState.Detached;
        }

        public void attach(ProcessCollectable process, AttachResultCallback callback)
        {
            Debug.Assert(attachState == AttachState.Detached, "Must be detached before attaching");

            Log.Info("attaching to process {0}", process);

            try
            {
                bool pipeServerStartRes = pipeServer.start();

                if(!pipeServerStartRes)
                {
                    callback(false, AttachResult.PipeServerStartup,
                        string.Format("Failed to start the pipe server for '{0}'. Reason: unknown", pipeServer.PipeName));
                    return;
                }
            }
            catch (IOException e)
            {
                callback(false, AttachResult.PipeServerStartup,
                    string.Format("Failed to start the pipe server for '{0}'. Reason: {1}", pipeServer.PipeName, e.Message));
                return;
            }

#if !WITHOUT_GAME
            DllInjectionResult dllResult = DLLInjector.Inject(process.Process, dllName);

            // DLL injection error handling;
            switch(dllResult)
            {
                case DllInjectionResult.Success:
                    break;
                case DllInjectionResult.DllNotFound:
                    pipeServer.stop();

                    callback(false, AttachResult.DllMissing,
                        string.Format("Failed to inject {0} in to {1} because {0} was missing from the current directory.", dllName, process));
                    return;
                case DllInjectionResult.GameProcessNotFound:
                    pipeServer.stop();

                    callback(false, AttachResult.DllNoProcess,
                        string.Format("Failed to inject {0} in to {1} because the process died before we could inject!", dllName, process));
                    return;
                case DllInjectionResult.InjectionFailed:
                    pipeServer.stop();

                    callback(false, AttachResult.DllInjectionFailure,
                        string.Format("Failed to inject {0} in to {1}. Possible bad DLL or process crash.", dllName, process));
                    return;
            }
#endif

            DllHandshake.process(pipeServer, process, (result, attachResult, message) =>
            {
                if (result)
                {
                    currentProcess = process;
                    attachState = AttachState.Attached;

                    // begin listening for events related to this process
                    Process p = process.Process;

                    p.EnableRaisingEvents = true;
                    p.Exited += new EventHandler(AttachedProcessExited);
                }
                else
                {
                    pipeServer.stop();
                }

                callback(result, attachResult, message);
            });
        }

        public void capture()
        {
            Debug.Assert(captureState == CaptureState.NotCapturing &&
                attachState == AttachState.Attached, "Must be attached and not capturing already before capturing");

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
