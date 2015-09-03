using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PSCap
{

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

    enum EventNotification
    {
        CaptureStarted,
        CaptureStarting,
        CaptureStopped,
        CaptureStopping,
        Detached,
        Detaching,
        Attached,
        Attaching
    }

    delegate void AttachResultCallback(bool okay, AttachResult result, string message);
    delegate void NewEventCallback(EventNotification evt, bool timeout);
    delegate void PendingMessageCallback(EventNotification evt, bool success);
    delegate void NewGameRecord(List<GameRecord> record);

    class CaptureLogic
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

        enum PendingMessageState
        {
            Detached,
            Attached,
            StartCaptureSent,
            StartCaptureReceieved,
            StopCaptureSent,
            StopCaptureReceived,
            DisconnectSent,
            DisconnectReceived,
        }

        AttachState attachState = AttachState.Detached;
        CaptureState captureState = CaptureState.NotCapturing;
        PendingMessageState pendingMessageState = PendingMessageState.Detached;
        PendingMessageCallback pendingMessageCallback;

        ProcessCollectable currentProcess;
        PipeServer pipeServer;
        string dllName;
        string pipeServerName;

        CancellationTokenSource receiveTaskCancel;
        Task receiveTask;


        // event notifications
        public event EventHandler AttachedProcessExited;
        public event NewEventCallback NewEvent;
        public event NewGameRecord OnNewRecord;

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

            notifyEvent(EventNotification.Detaching);

            if (captureState == CaptureState.Capturing)
            {
                Log.Info("currently capturing...stopping capture first");
                stopCapture((evt, success) =>
                {
                    Log.Info("Completed stopCapture");
                });
            }
            else
            {
                Log.Info("wasn't capturing...going ahead with detach");
            }

            receiveTaskCancel.Cancel();
            if(!receiveTask.IsFaulted)
                receiveTask.Wait();
            receiveTask = null;

            pipeServer.stop();
            currentProcess.Process.Close();

            notifyEvent(EventNotification.Detached);
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

                    receiveTaskCancel = new CancellationTokenSource();

                    // start receive thread
                    receiveTask = Task.Factory.StartNew(async delegate
                    {
                        Log.Debug("Receive task started...");

                        while (!receiveTaskCancel.IsCancellationRequested)
                        {
                            List<byte> msg = await pipeServer.readMessageAsync(receiveTaskCancel.Token);
                            
                            if (msg != null)
                            {
                                DllMessage msgDecoded = DllMessage.Factory.Decode(new BitStream(msg));

                                if (msgDecoded != null)
                                    handleMessage(msgDecoded);
                                else
                                    Log.Error("Failed to decode message");
                            }
                            else
                            {
                                await Task.Run(() => { if (isAttached()) detach(); });
                            }
                        }

                        Log.Debug("Receive task stopped...");
                    }, receiveTaskCancel.Token);
                }
                else
                {
                    pipeServer.stop();
                }

                callback(result, attachResult, message);
            });
        }

        public async void capture()
        {
            Debug.Assert(captureState == CaptureState.NotCapturing &&
                attachState == AttachState.Attached, "Must be attached and not capturing already before capturing");
            Log.Info("capturing data from process {0}", currentProcess);

            notifyEvent(EventNotification.CaptureStarting);

            DllMessageStartCapture msg = DllMessage.Factory.Create(DllMessageType.START_CAPTURE) as DllMessageStartCapture;

            msg.reason = DllMessageStartCapture.DllMessageStartCaptureReason.UserRequest;

            await Task.Factory.StartNew(() =>
            {
                pipeServer.writeMessage(DllMessage.Factory.Encode(msg).data, (result) =>
                {
                    if (!result)
                        return;

                    pendingMessageState = PendingMessageState.StartCaptureSent;
                }, TimeSpan.FromMilliseconds(1000));
            });
        }

        public async void stopCapture(PendingMessageCallback callback = null)
        {
            Debug.Assert(captureState == CaptureState.Capturing &&
                attachState == AttachState.Attached, "Must be attached before capturing");
            Log.Info("stopping data capture from process {0}", currentProcess);

            notifyEvent(EventNotification.CaptureStopping);

            DllMessageStopCapture msg = DllMessage.Factory.Create(DllMessageType.STOP_CAPTURE) as DllMessageStopCapture;

            msg.reason = DllMessageStopCapture.DllMessageStopCaptureReason.UserRequest;

            await Task.Factory.StartNew(() =>
            {
                if (callback != null)
                    pendingMessageCallback = callback;

                pipeServer.writeMessage(DllMessage.Factory.Encode(msg).data, (result) =>
                {
                    if (!result)
                        return;

                    pendingMessageState = PendingMessageState.StopCaptureSent;
                }, TimeSpan.FromMilliseconds(1000));
            });
        }

        private void handleMessage(DllMessage msg)
        {
            Log.Info("Got message of type " + msg.type.ToString());

            switch(msg.type)
            {
                case DllMessageType.NEW_RECORDS:
                    {
                        DllMessageNewRecords m = msg as DllMessageNewRecords;

                        OnNewRecord(m.records);

                        break;
                    }
                case DllMessageType.START_CAPTURE_RESP:
                    if(pendingMessageState == PendingMessageState.StartCaptureSent)
                    {
                        DllMessageStartCaptureResp m = msg as DllMessageStartCaptureResp;

                        if(m.okay)
                        {
                            Log.Info("DLL confirms capture start");

                            captureState = CaptureState.Capturing;
                            notifyEvent(EventNotification.CaptureStarted);
                        }

                        if (pendingMessageCallback != null)
                        {
                            pendingMessageCallback(EventNotification.CaptureStarted, m.okay);
                            pendingMessageCallback = null;
                        }

                        pendingMessageState = PendingMessageState.Attached;
                    }

                    break;
                case DllMessageType.STOP_CAPTURE_RESP:
                    if (pendingMessageState == PendingMessageState.StopCaptureSent)
                    {
                        DllMessageStopCaptureResp m = msg as DllMessageStopCaptureResp;

                        if (m.okay)
                        {
                            Log.Info("DLL confirms capture stop");

                            captureState = CaptureState.NotCapturing;
                            notifyEvent(EventNotification.CaptureStopped);
                        }

                        if (pendingMessageCallback != null)
                        {
                            pendingMessageCallback(EventNotification.CaptureStopped, m.okay);
                            pendingMessageCallback = null;
                        }

                        pendingMessageState = PendingMessageState.Attached;
                    }

                    break;
            }
        }

        private void notifyEvent(EventNotification evt)
        {
            NewEvent.Invoke(evt, false);
        }

        private void notifyEventTimeout(EventNotification evt)
        {
            NewEvent.Invoke(evt, true);
        }
    }
}
