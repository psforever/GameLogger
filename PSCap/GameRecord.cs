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

        protected abstract bool decode(BitStream stream);

        protected abstract List<byte> encode();

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
                    rec.decode(stream);
                }
                catch (InvalidOperationException e)
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
