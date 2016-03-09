using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCap
{
    enum GameRecordType
    {
        CRYPTO_STATE = 0,
        PACKET
    }

    abstract class GameRecord
    {
        public GameRecordType type { get; private set; }
        private ulong recordTime;

        public abstract long getEstimatedSize();

        protected abstract bool decode(BitStream stream);

        protected abstract List<byte> encode();

        public ulong getRecordTime()
        {
            return recordTime;
        }

        public void setTimeStamp(ulong ticks)
        {
            recordTime = ticks;
        }

        public static class Factory
        {
            public static GameRecord Create(GameRecordType type)
            {
                GameRecord rec = null;

                switch (type)
                {
                    case GameRecordType.CRYPTO_STATE:
                        rec = new GameRecordCryptoState();
                        break;
                    case GameRecordType.PACKET:
                        rec = new GameRecordPacket();
                        break;
                    default:
                        throw new ArgumentException(string.Format("GameRecord.Create: unknown GameRecord type {0}", type));
                }

                // msg is guaranteed to be non-null due to default case throw
                rec.type = type;

                return rec;
            }

            public static GameRecord Decode(BitStream stream)
            {
                if (stream.sizeLeft() < 1)
                    return null;

                GameRecordType type = (GameRecordType)BitOps.ReadByte(stream);
                GameRecord rec = null;

                // decoding shouldnt fail with an exception, but Create can be static
                // in code and should definitely fail
                try
                {
                    rec = Create(type);
                }
                catch (ArgumentException e)
                {
                    Log.Error("Failed to decode GameRecord: {0}", e.Message);
                    return null;
                }

                // handle any decoding errors
                try
                {
                    // first decode the record timestamp
                    // will fail if it fails
                    rec.setTimeStamp(BitOps.ReadUInt64(stream));

                    bool decodeResult = rec.decode(stream);

                    if (!decodeResult)
                        throw new InvalidOperationException();
                }
                catch (InvalidOperationException)
                {
                    Log.Error("Failed to decode GameRecord: general decoding error");
                    return null;
                }

                return rec;
            }

            public static List<byte> Encode(GameRecord rec)
            {
                List<byte> bytes = null;

                try
                {
                    bytes = rec.encode();
                }
                catch (InvalidOperationException e)
                {
                    Log.Error("Failed to encode GameRecord: {0}", e.Message);
                    return null;
                }

                // add in the timestamp
                List<byte> timeStamp = new List<byte>();
                BitOps.WriteUInt64(timeStamp, rec.getRecordTime());
                bytes.InsertRange(0, timeStamp);

                // add in the opcode
                bytes.Insert(0, (byte)rec.type);

                return bytes;
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
    
    class GameRecordCryptoState : GameRecord
    {
        public override long getEstimatedSize()
        {
            return 0;
        }

        protected override bool decode(BitStream stream)
        {
            //reason = (DisconnectReason)BitOps::ReadByte(stream);

            return true;
        }

        protected override List<byte> encode()
        {
            List<byte> encoded = new List<byte>();

            //BitOps::WriteByte(*encoded, reason);

            return encoded;
        }
    };

    class GameRecordPacket : GameRecord
    {
        public enum PacketType
        {
            Login = 0,
            Game
        }

        public enum PacketDestination
        {
            Server = 0,
            Client
        }

        public PacketType packetType;
        public PacketDestination packetDestination;
        public List<byte> packet;

        public override long getEstimatedSize()
        {
            return packet.Count + 2 + sizeof(ulong);
        }

        protected override bool decode(BitStream stream)
        {
            packetType = (PacketType)BitOps.ReadByte(stream);
            packetDestination = (PacketDestination)BitOps.ReadByte(stream);
            packet = BitOps.DecodeOctetStream(stream);

            return true;
        }

        protected override List<byte> encode()
        {
            List<byte> encoded = new List<byte>();

            BitOps.WriteByte(encoded, (byte)packetType);
            BitOps.WriteByte(encoded, (byte)packetDestination);
            BitOps.EncodeOctetStream(encoded, packet);

            return encoded;
        }
    };
    
}
