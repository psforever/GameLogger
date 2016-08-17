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
                Log.Error("Failed to read message from DLL");
                smartCallback(false, AttachResult.DllHandshake, "DLL did not send a handshake");
                return;
            }

            Log.Debug("Read message of size " + message.Count);

            DllMessageIdentify msg = DllMessage.Factory.Decode(new BitStream(message)) as DllMessageIdentify;

            if (msg == null)
            {
                smartCallback(false, AttachResult.DllHandshake, "Failed to decode the DLL identity");
                return;
            }

            Log.Info("DLL version {0}.{1}, PID {2}",
                msg.majorVersion, msg.minorVersion, msg.pid);

            if (PSCapMain.GAME_LOGGER_MAJOR_VERSION != msg.majorVersion ||
                PSCapMain.GAME_LOGGER_MINOR_VERSION != msg.minorVersion)
            {
                smartCallback(false, AttachResult.DllHandshake, string.Format("DLL version is incompatible.\n" +
                    "Required version {0}.{1}, got {2}.{3}",
                    PSCapMain.GAME_LOGGER_MAJOR_VERSION, PSCapMain.GAME_LOGGER_MINOR_VERSION,
                    msg.majorVersion, msg.minorVersion));
                return;
            }

            // verify that the DLL has returned the correct PID
#if !WITHOUT_GAME
            bool processMatch = process.Process.Id == msg.pid;
#else
            bool processMatch = true;
#endif

            if (!processMatch)
            {
                smartCallback(false, AttachResult.DllHandshake, "DLL did not send the right PID");
                return;
            }

            DllMessageIdentifyResp msgResp = DllMessage.Factory.Create(DllMessageType.IDENTIFY_RESP) as DllMessageIdentifyResp;
            msgResp.accepted = true;

            pipeServer.writeMessage(DllMessage.Factory.Encode(msgResp).data, (resultWrite) =>
            {
                handleWriteMessage(smartCallback, resultWrite);
            }, TimeSpan.FromMilliseconds(1000));
        }

        private static void handleWriteMessage(AttachResultCallback callback, bool result)
        {
            if (result)
            {
                callback(true, AttachResult.Success, "");
            }
            else
            {
                Log.Error("Failed to write message to DLL");
                callback(false, AttachResult.DllHandshake, "DLL didn't receive the handshake response");
            }
        }
    }
}
