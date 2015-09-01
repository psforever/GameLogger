using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCap
{
    enum DllMessageType
    {
        DISCONNECT = 0, // Either
        DISCONNECT_RESP, // Either
        IDENTIFY, // FROM
        IDENTIFY_RESP, // TO
        START_CAPTURE, // Either
        START_CAPTURE_RESP, // Either
        STOP_CAPTURE, // Either
        STOP_CAPTURE_RESP, // Either
        NEW_RECORD, // FROM
    }

    abstract class DllMessage
    {
        public DllMessageType type { get; private set; }

        protected abstract bool decode(BitStream stream);

        protected abstract List<byte> encode();

        public static class Factory
        {
            public static DllMessage Create(DllMessageType type)
            {
                DllMessage msg = null;

                switch (type)
                {
                    case DllMessageType.DISCONNECT:
                        msg = new DllMessageDisconnect();
                        break;
                    case DllMessageType.DISCONNECT_RESP:
                        msg = new DllMessageDisconnectResp();
                        break;
                    case DllMessageType.IDENTIFY:
                        msg = new DllMessageIdentify();
                        break;
                    case DllMessageType.IDENTIFY_RESP:
                        msg = new DllMessageIdentifyResp();
                        break;
                    case DllMessageType.START_CAPTURE:
                        msg = new DllMessageStartCapture();
                        break;
                    case DllMessageType.START_CAPTURE_RESP:
                        msg = new DllMessageStartCaptureResp();
                        break;
                    case DllMessageType.STOP_CAPTURE:
                        msg = new DllMessageStopCapture();
                        break;
                    case DllMessageType.STOP_CAPTURE_RESP:
                        msg = new DllMessageStopCaptureResp();
                        break;
                    case DllMessageType.NEW_RECORD:
                        msg = new DllMessageNewRecord();
                        break;
                    default:
                        throw new ArgumentException(string.Format("DllMessage.Create: Unhandled DllMessage type {0}", type));
                }

                // msg is guaranteed to be non-null due to default case throw
                msg.type = type;

                return msg;
            }

            public static DllMessage Decode(BitStream stream)
            {
                if (stream.sizeLeft() < 1)
                    return null;

                DllMessageType opcode = (DllMessageType)BitOps.ReadByte(stream);
                DllMessage msg = null;

                // decoding shouldnt fail with an exception, but Create can be static
                // in code and should definitely fail
                try
                {
                    msg = Create(opcode);
                }
                catch(ArgumentException e)
                {
                    Log.Error("Failed to decode message: {0}", e.Message);
                    return null;
                }

                // handle any decoding errors
                try
                {
                    msg.decode(stream);
                }
                catch(InvalidOperationException e)
                {
                    Log.Error("Failed to decode message: general decoding error");
                    return null;
                }

                return msg;
            }

            public static BitStream Encode(DllMessage msg)
            {

                List<byte> bytes = null;

                try
                {
                    bytes = msg.encode();
                }
                catch(InvalidOperationException e)
                {
                    Log.Error("Failed to encode message: {0}", e.Message);
                    return null;
                }

                // add in the opcode
                bytes.Insert(0, (byte)msg.type);
                

                return new BitStream(bytes);
            }
        }

        protected bool decodeNotImplemented()
        {
            throw new InvalidOperationException("decode not implemented");
        }

        protected List<byte> encodeNotImplemented()
        {
            throw new InvalidOperationException("encode not implemented");
        }
    }

    class DllMessageDisconnect : DllMessage
    {
        public enum DisconnectReason
        {
            Detach = 0,
            ProgramClosing = 1,
            Unknown
        }

        public DisconnectReason reason;

        protected override bool decode(BitStream stream)
        {
            reason = (DisconnectReason)BitOps.ReadByte(stream);

            return true;
        }

        protected override List<byte> encode()
        {
            List<byte> encoded = new List<byte>();

            encoded.AddRange(BitConverter.GetBytes((byte)reason));

            return encoded;
        }
    }

    class DllMessageDisconnectResp : DllMessage
    {
        protected override bool decode(BitStream stream)
        {
            return true;
        }

        protected override List<byte> encode()
        {
            List<byte> encoded = new List<byte>();

            return encoded;
        }
    }

    class DllMessageIdentify : DllMessage
    {
        public byte majorVersion;
        public byte minorVersion;
        public byte revision;
        public uint pid;

        protected override bool decode(BitStream stream)
        {
            majorVersion = BitOps.ReadByte(stream);
            minorVersion = BitOps.ReadByte(stream);
            revision = BitOps.ReadByte(stream);
            pid = BitOps.ReadUInt32(stream);

            return true;
        }

        protected override List<byte> encode()
        {
            return encodeNotImplemented();
        }
    }

    class DllMessageIdentifyResp : DllMessage
    {
        public bool accepted;

        protected override bool decode(BitStream stream)
        {
            return decodeNotImplemented();
        }

        protected override List<byte> encode()
        {
            List<byte> encoded = new List<byte>();

            encoded.AddRange(BitConverter.GetBytes(accepted));
            
            return encoded;
        }
    }

    class DllMessageStartCapture : DllMessage
    {
        public enum DllMessageStartCaptureReason
        {
            UserRequest = 0,
            Unknown
        }

        public DllMessageStartCaptureReason reason;

        protected override bool decode(BitStream stream)
        {
            reason = (DllMessageStartCaptureReason)BitOps.ReadByte(stream);

            return true;
        }

        protected override List<byte> encode()
        {
            List<byte> encoded = new List<byte>();

            encoded.AddRange(BitConverter.GetBytes((byte)reason));

            return encoded;
        }
    }

    class DllMessageStartCaptureResp : DllMessage
    {
        public enum DllMessageStartCaptureError
        {
            NotCapturing = 0,
            Unknown
        }

        public bool okay;
        public DllMessageStartCaptureError errorType;

        protected override bool decode(BitStream stream)
        {
            okay = BitOps.ReadBool(stream);
            errorType = (DllMessageStartCaptureError)BitOps.ReadByte(stream);

            return true;
        }

        protected override List<byte> encode()
        {
            List<byte> encoded = new List<byte>();

            encoded.AddRange(BitConverter.GetBytes(okay));
            encoded.AddRange(BitConverter.GetBytes((byte)errorType));

            return encoded;
        }
    }

    class DllMessageStopCapture : DllMessage
    {
        public enum DllMessageStopCaptureReason
        {
            UserRequest = 0,
            ProgramExiting,
            Unknown
        }

        public DllMessageStopCaptureReason reason;

        protected override bool decode(BitStream stream)
        {
            reason = (DllMessageStopCaptureReason)BitOps.ReadByte(stream);

            return true;
        }

        protected override List<byte> encode()
        {
            List<byte> encoded = new List<byte>();

            encoded.AddRange(BitConverter.GetBytes((byte)reason));

            return encoded;
        }
    }

    class DllMessageStopCaptureResp : DllMessage
    {
        public enum DllMessageStopCaptureError
        {
            NotCapturing = 0,
            Unknown
        }

        public bool okay;
        public DllMessageStopCaptureError errorType;

        protected override bool decode(BitStream stream)
        {
            okay = BitOps.ReadBool(stream);
            errorType = (DllMessageStopCaptureError)BitOps.ReadByte(stream);

            return true;
        }

        protected override List<byte> encode()
        {
            List<byte> encoded = new List<byte>();

            encoded.AddRange(BitConverter.GetBytes(okay));
            encoded.AddRange(BitConverter.GetBytes((byte)errorType));

            return encoded;
        }
    }

    class DllMessageNewRecord : DllMessage
    {
        public GameRecord record;

        protected override bool decode(BitStream stream)
        {
            GameRecord newRecord = GameRecord.Factory.Decode(stream);

            if (newRecord == null)
                return false;

            record = newRecord;

            return true;
        }

        protected override List<byte> encode()
        {
            if (record == null)
                throw new InvalidOperationException("GameRecord has not been set");

            return GameRecord.Factory.Encode(record);
        }
    }
}
