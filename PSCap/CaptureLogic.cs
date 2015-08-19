using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            attachState = AttachState.Detached;
        }

        public void attach(ProcessCollectable process, Action <bool, AttachResult, string> callback)
        {
            Debug.Assert(attachState == AttachState.Detached, "Must be detached before attaching");

            Log.Info("attaching to process {0}", process);

            // begin listening for events related to this process
            Process p = process.Process;

            p.EnableRaisingEvents = true;
            p.Exited += new EventHandler(AttachedProcessExited);

            try
            {
                bool pipeServerStartRes = pipeServer.start();

                if(!pipeServerStartRes)
                {
                    callback.Invoke(false, AttachResult.PipeServerStartup,
                        string.Format("Failed to start the pipe server for '{0}'. Reason: unknown", pipeServer.PipeName));
                    return;
                }
            }
            catch (IOException e)
            {
                callback.Invoke(false, AttachResult.PipeServerStartup,
                    string.Format("Failed to start the pipe server for '{0}'. Reason: {1}", pipeServer.PipeName, e.Message));
                return;
            }

            DllInjectionResult dllResult = DLLInjector.Inject(p, Path.Combine(Environment.CurrentDirectory, "pslog.dll"));

            // DLL injection error handling;
            switch(dllResult)
            {
                case DllInjectionResult.Success:
                    break;
                case DllInjectionResult.DllNotFound:
                    pipeServer.stop();

                    callback.Invoke(false, AttachResult.DllMissing,
                        string.Format("Failed to inject {0} in to {1} because {0} was missing from the current directory.", dllName, process));
                    return;
                case DllInjectionResult.GameProcessNotFound:
                    pipeServer.stop();

                    callback.Invoke(false, AttachResult.DllNoProcess,
                        string.Format("Failed to inject {0} in to {1} because the process died before we could inject!", dllName, process));
                    return;
                case DllInjectionResult.InjectionFailed:
                    pipeServer.stop();

                    callback.Invoke(false, AttachResult.DllInjectionFailure,
                        string.Format("Failed to inject {0} in to {1}. Possible bad DLL or process crash.", dllName, process));
                    return;
            }

            // fire off our attach task
            pipeServer.waitForConnection((okay) =>
            {
                if(!okay)
                {
                    pipeServer.stop();

                    Log.Info("Failed to receive a pipe connection before timeout");
                    callback.Invoke(false, AttachResult.DllConnection,
                        string.Format("Failed to receive a pipe connection before timeout. This could indicate a DLL crash or hang"));
                }
                else
                {
                    Log.Info("Got pipe connection. Now communicating with DLL");

                    pipeServer.readMessage((result, message) =>
                    {
                        if(result)
                        {
                            Log.Info("Read message of size " + message.Count);

                            // success case!
                            callback.Invoke(true, AttachResult.Success, "");
                            currentProcess = process;
                            attachState = AttachState.Attached;
                        }
                        else
                        {
                            pipeServer.stop();

                            Log.Info("Failed to read message from DLL");
                            callback.Invoke(false, AttachResult.DllHandshake, "DLL did not complete the correct handshake");
                        }
                        
                    }, TimeSpan.FromMilliseconds(1000));
                }

            }, TimeSpan.FromMilliseconds(1000));
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
