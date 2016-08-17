using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSCap
{
    class PipeServer
    {
        NamedPipeServerStream pipeServer;
        public string PipeName { get; }
        bool serverStarted = false;
        private byte[] pipeBuffer;

        public PipeServer(string listeningPipe)
        {
            this.PipeName = listeningPipe;
            this.pipeBuffer = new byte[100000];
        }

        public bool start()
        {
            Trace.Assert(!serverStarted, "Tried to start the pipe server twice");
            Trace.Assert(pipeServer == null, "Pipe server instance is not null");

            // callee should handle exceptions
            pipeServer = new NamedPipeServerStream(PipeName,
                PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

            serverStarted = true;
            return true;
        }

        public void stop()
        {
            Trace.Assert(serverStarted, "Pipe server cannot be stopped as it wasn't running");

            pipeServer.Close();
            pipeServer.Dispose();
            pipeServer = null;
            serverStarted = false;
        }

        public void stopBlocking()
        {
            if (serverStarted)
            {
                Log.Warning("stopping pipe server in a blocking way");

                if (pipeServer.IsConnected)
                    pipeServer.Disconnect();

                pipeServer.Close();
                pipeServer.Dispose();
                pipeServer = null;
                serverStarted = false;
            }
        }

        public void restart()
        {
            stop();
            start();
        }

        public bool waitForConnection(TimeSpan timeout)
        {
            try
            {
                var asyncResult = pipeServer.BeginWaitForConnection(null, this);

                if(asyncResult.AsyncWaitHandle.WaitOne(timeout))
                {
                    pipeServer.EndWaitForConnection(asyncResult);
                }
                else
                {
                    throw new TimeoutException("connection wait timed out");
                }

                return true;
            }
            catch(TimeoutException e)
            {
                Log.Info("Got timeout exception: " + e.Message);
                return false;
            }
        }

        public void readMessage(Action<bool, List<byte>> callback, TimeSpan timeout)
        {
            if (!serverStarted)
                throw new InvalidOperationException("Tried to read message from a disconnected pipe");

            try
            {
                List<byte> outBuf = new List<byte>(1000);
                
                int messageSize = 0;
                byte[] tmpBuf = new byte[1000];

                do
                {
                    Log.Debug("ReadMessage start", outBuf.Count);

                    int readAmount = 0;
                    var asyncResult = pipeServer.BeginRead(tmpBuf, 0, tmpBuf.Length, null, null);

                    if (asyncResult.AsyncWaitHandle.WaitOne(timeout))
                        readAmount = pipeServer.EndRead(asyncResult);
                    else
                        break;

                    if (readAmount == 0)
                        break;
                    
                    outBuf.AddRange(new SegmentEnumerable(new ArraySegment<byte>(tmpBuf, 0, readAmount)));
                    Log.Debug("ReadMessage total {0}", outBuf.Count);
                    messageSize += readAmount;
                } while (!pipeServer.IsMessageComplete);

                if (messageSize > 0)
                    callback(true, outBuf);
                else
                    callback(false, null);
            }
            catch (IOException e)
            {
                Log.Info("Got IOException: " + e.Message);

                callback(false, null);
            }
        }

        public async Task<List<Byte>> readMessageAsync(CancellationToken token)
        {
            if (!serverStarted)
                throw new InvalidOperationException("Tried to read message from a disconnected pipe");

            return await Task.Run(async delegate
            {
                try
                {
                    List<byte> outBuf = new List<byte>(1000);

                    int messageSize = 0;

                    do
                    {
                        int readAmount = await pipeServer.ReadAsync(pipeBuffer, 0, pipeBuffer.Length, token);

                        if (readAmount == 0)
                            break;

                        outBuf.AddRange(new SegmentEnumerable(new ArraySegment<byte>(pipeBuffer, 0, readAmount)));
                        messageSize += readAmount;
                    } while (!pipeServer.IsMessageComplete);

                    if (messageSize > 0)
                        return outBuf;
                    else
                        return null;
                }
                catch (IOException e)
                {
                    Log.Info("Got IOException: " + e.Message);

                    return null;
                }
            });
        }

        public void writeMessage(byte[] message, Action<bool> callback, TimeSpan timeout)
        {
            if (!serverStarted)
                throw new InvalidOperationException("Tried to write message to a disconnected pipe");

            try
            {
                var asyncResult = pipeServer.BeginWrite(message, 0, message.Length, null, null);

                if (asyncResult.AsyncWaitHandle.WaitOne(timeout))
                {
                    pipeServer.EndWrite(asyncResult);
                    callback(true);
                }
                else
                {
                    callback(false);
                }
            }
            catch (IOException e)
            {
                Log.Info("Got IOException: " + e.Message);

                callback(false);
            }
        }

        public class SegmentEnumerable : IEnumerable<byte>, IEnumerable
        {
            private ArraySegment<byte> segment;

            public SegmentEnumerable(ArraySegment<byte> segment)
            {
                this.segment = segment;
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
            {
                return GetEnumerator();
            }

            public SegmentEnumerator GetEnumerator()
            {
                return new SegmentEnumerator(segment);
            }

            public struct SegmentEnumerator : IEnumerator<byte>
            {
                byte[] Array;
                int Index;
                int End;

                internal SegmentEnumerator(ArraySegment<byte> segment)
                {
                    Array = segment.Array;
                    Index = segment.Offset - 1;
                    End = segment.Offset + segment.Count;
                }

                public bool MoveNext()
                {
                    return ++Index < End;
                }

                public byte Current
                {
                    get { return Array[Index]; }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                public void Dispose()
                {
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
