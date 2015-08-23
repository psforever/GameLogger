using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCap
{
    class DllHandshake
    {
        public static async void process(PipeServer pipeServer, ProcessCollectable process, AttachResultCallback callback)
        {
            // fire off our attach task
            // NOTE: you must await here in order for exceptions to be passed up
            await Task.Factory.StartNew(() =>
            {
                bool connected = pipeServer.waitForConnection(TimeSpan.FromMilliseconds(3000));

                if (!connected)
                {
                    Log.Info("Failed to receive a pipe connection before timeout");
                    callback(false, AttachResult.DllConnection,
                       string.Format("Failed to receive a pipe connection before timeout. This could indicate a DLL hanging with an open handle."));
                }
                else
                {
                    Log.Info("Got pipe connection. Now communicating with DLL");

                    pipeServer.readMessage((result, message) =>
                   {
                       handleReadMessage(pipeServer, process, callback, result, message);
                   }, TimeSpan.FromMilliseconds(1000));
                }
            });
        }

        private static void handleReadMessage(PipeServer pipeServer, ProcessCollectable process,
            AttachResultCallback smartCallback, bool result, List<byte> message)
        {
            if (!result)
            {
                Log.Info("Failed to read message from DLL");
                smartCallback(false, AttachResult.DllHandshake, "DLL did not send a handshake");
                return;
            }

            Log.Info("Read message of size " + message.Count);

            DllMessageIdentify msg = DllMessage.Factory.Decode(new BitStream(message)) as DllMessageIdentify;

            if (msg == null)
            {
                smartCallback(false, AttachResult.DllHandshake, "Failed to decode the DLL identity");
                return;
            }

            Log.Info("DLL version {0}.{1}.{2}, pid {3}",
                msg.majorVersion, msg.minorVersion, msg.revision, msg.pid);

            // verify that the DLL has returned the correct PID
            // TODO: verify that this DLL version is compatible with the current logger version

            DllMessageIdentifyResp msgResp = DllMessage.Factory.Create(DllMessageType.IDENTIFY_RESP) as DllMessageIdentifyResp;
#if !WITHOUT_GAME
            bool processMatch = process.Process.Id == msg.pid;
#else
            bool processMatch = true;
#endif

            msgResp.accepted = processMatch;

            pipeServer.writeMessage(DllMessage.Factory.Encode(msgResp).data, (resultWrite) =>
            {
                handleWriteMessage(processMatch, smartCallback, resultWrite);
            }, TimeSpan.FromMilliseconds(1000));
        }

        private static void handleWriteMessage(bool processMatch, AttachResultCallback callback, bool result)
        {
            if (result)
            {
                // success case!
                if (processMatch)
                    callback(true, AttachResult.Success, "");
                else
                    callback(false, AttachResult.DllHandshake, "DLL did not send the right PID");
            }
            else
            {
                Log.Info("Failed to write message to DLL");
                callback(false, AttachResult.DllHandshake, "DLL didn't receive the handshake response");
            }
        }
    }
}
