using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSCap
{
    public static class TaskExtensions
    {
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            var timeoutCancellationTokenSource = new CancellationTokenSource();

            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token)).ConfigureAwait(false);
            if (completedTask == task)
            {
                timeoutCancellationTokenSource.Cancel();
                return await task;
            }
            else
            {
                throw new TimeoutException("The operation has timed out.");
            }
        }
    }

    class PipeServer
    {
        NamedPipeServerStream pipeServer;
        public string PipeName { get; }
        bool serverStarted = false;

        public PipeServer(string listeningPipe)
        {
            this.PipeName = listeningPipe;
        }

        public bool start()
        {
            if (serverStarted)
            {
                Log.Warning("Tried to start the pipe server twice");
                return true;
            }

            // callee should handle exceptions
            NamedPipeServerStream server = new NamedPipeServerStream(PipeName,
                PipeDirection.InOut, 1, PipeTransmissionMode.Message);
                
            pipeServer = server;

            serverStarted = true;
            return true;
        }

        public bool waitForConnection(TimeSpan timeout)
        {
            try
            {
                Log.Info("starting wait");
                var theTask = Task<bool>.Factory.StartNew(() =>
                {
                    pipeServer.WaitForConnection();
                    return true;
                }).TimeoutAfter(timeout);

                theTask.Wait();
                theTask.Dispose();

                return true;
            }
            catch(AggregateException e)
            {
                foreach(Exception ee in e.InnerExceptions)
                {
                    Log.Info("Got timeout exception: " + ee.Message);
                }

                return false;
            }
        }

        public void readMessage(Action<bool, List<byte>> callback, TimeSpan timeout)
        {
            if (!serverStarted)
                throw new InvalidOperationException("Tried to read message from a disconnected pipe");

            try
            {
                Log.Info("starting read");

                List<byte> outBuf = new List<byte>(100);
                byte[] tmpBuf = new byte[100]; 
                int messageSize = 0;

                Task<bool>.Factory.StartNew(() =>
                {
                    do
                    {
                        int readAmount = pipeServer.Read(tmpBuf, 0, tmpBuf.Length);

                        if (readAmount == 0)
                            return false;

                        outBuf.AddRange(new SegmentEnumerable(new ArraySegment<byte>(tmpBuf, 0, readAmount)));
                        messageSize += readAmount;
                    } while (!pipeServer.IsMessageComplete);

                    return true;
                }).TimeoutAfter(timeout).Wait();

                if (messageSize > 0)
                    callback(true, outBuf);
                else
                    callback(false, null);
            }
            catch (AggregateException e)
            {
                foreach (Exception ee in e.InnerExceptions)
                {
                    Log.Info("Got timeout exception: " + ee.Message);
                }

                callback(false, null);
            }
        }

        public void writeMessage(byte[] message, Action<bool> callback, TimeSpan timeout)
        {
            if (!serverStarted)
                throw new InvalidOperationException("Tried to write message to a disconnected pipe");

            try
            {
                Log.Info("starting write");

                Task<bool>.Factory.StartNew(() =>
                {
                    pipeServer.Write(message, 0, message.Length);
                    return true;
                }).TimeoutAfter(timeout).Wait();

                callback(true);
            }
            catch (AggregateException e)
            {
                foreach (Exception ee in e.InnerExceptions)
                {
                    Log.Info("Got timeout exception: " + ee.Message);
                }

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

        public void stop()
        {
            if(serverStarted)
            {
                if(pipeServer.IsConnected)
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
    }
}
