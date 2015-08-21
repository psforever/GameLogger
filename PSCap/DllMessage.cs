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
        IDENTIFY = 0,
        IDENTIFY_RESPONSE = 1,
    }

    abstract class DllMessage
    {
        public abstract bool decode(BitStream stream);
        public abstract BitStream encode();

        public static class Factory
        {
            public static DllMessage Decode(BitStream stream)
            {
                DllMessage msg = null;

                if (stream.sizeLeft() < 1)
                    return null;

                DllMessageType opcode = (DllMessageType)BitOps.ReadByte(stream);

                switch (opcode)
                {
                    case DllMessageType.IDENTIFY:
                        msg = new DllMessageIdentify();
                        break;
                    case DllMessageType.IDENTIFY_RESPONSE:
                        msg = new DllMessageIdentify();
                        break;
                }

                if (msg != null)
                {
                    if (!msg.decode(stream))
                        return null;
                }

                return msg;
            }
        }

        protected bool decodeNotImplemented()
        {
            Debug.Assert(false, "decode not implemented");
            return false;
        }

        protected BitStream encodeNotImplemented()
        {
            Debug.Assert(false, "encode not implemented");
            return null;
        }
    }

    class DllMessageIdentify : DllMessage
    {
        public byte majorVersion;
        public byte minorVersion;
        public byte revision;
        public uint pid;

        public override bool decode(BitStream stream)
        {
            try
            {
                majorVersion = BitOps.ReadByte(stream);
                minorVersion = BitOps.ReadByte(stream);
                revision = BitOps.ReadByte(stream);
                pid = BitOps.ReadUInt32(stream);

                return true;
            }
            catch(InvalidOperationException e)
            {
                return false;
            }
        }

        public override BitStream encode()
        {
            return encodeNotImplemented();
        }
    }

    class DllMessageIdentifyResp : DllMessage
    {
        public bool accepted;

        public override bool decode(BitStream stream)
        {
            return decodeNotImplemented();
        }

        public override BitStream encode()
        {
            List<byte> encoded = new List<byte>(BitConverter.GetBytes(accepted));
            
            encoded.Insert(0, (byte)DllMessageType.IDENTIFY_RESPONSE);

            return new BitStream(encoded);
        }
    }
}
