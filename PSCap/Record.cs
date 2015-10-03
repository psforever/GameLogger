using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCap
{
    enum RecordType
    {
        METADATA = 0,
        GAME = 1
    }

    public class NeedMoreDataException : Exception
    {
        public ulong bytesNeeded { get; private set; }

        public NeedMoreDataException(ulong bytesNeeded)
        {
            this.bytesNeeded = bytesNeeded;
        }
    }

    abstract class Record
    {
        public RecordType type { get; private set; }

        // unfortunately needed for file streaming
        public UInt32 size { get; protected set; }

        protected abstract bool decode(BitStream stream);

        protected abstract List<byte> encode();

        public static class Factory
        {
            public static Record Create(RecordType type)
            {
                Record rec = null;

                switch (type)
                {
                    case RecordType.METADATA:
                        rec = new RecordMetadata();
                        break;
                    case RecordType.GAME:
                        rec = new RecordGame();
                        break;
                    default:
                        throw new ArgumentException(string.Format("Record.Create: Unhandled Record type {0}", type));
                }

                // rec is guaranteed to be non-null due to default case throw
                rec.type = type;
                rec.size = 0;

                return rec;
            }

            public static Record Decode(BitStream stream)
            {
                // must have at least the record type along with the record length
                if (stream.sizeLeft() < sizeof(byte) + sizeof(UInt32))
                    throw new NeedMoreDataException((ulong)(sizeof(byte) + sizeof(UInt32) - stream.sizeLeft()));
                
                RecordType type = (RecordType)BitOps.ReadByte(stream);
                UInt32 size = BitOps.ReadUInt32(stream);

                if (size > stream.sizeLeft())
                    throw new NeedMoreDataException((ulong)(size - stream.sizeLeft()));

                // any exception or error past this point is an actual decoding error,
                // not a lack of bytes
                Record rec = Create(type);
                bool decodeResult = rec.decode(stream);

                if (!decodeResult)
                    throw new InvalidOperationException("Failed to decode inner record");

                rec.size = size;
            
                return rec;
            }

            public static BitStream Encode(Record rec)
            {
                List<byte> bytes = null;

                bytes = rec.encode();
                rec.size = (uint)bytes.Count;

                // NOTE: these are reversed because we are inserting, which is like a stack
                // push the byte count on
                bytes.InsertRange(0, BitConverter.GetBytes(rec.size));
                // add in the record type
                bytes.Insert(0, (byte)rec.type);

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

    class RecordMetadata : Record
    {
        public string captureName = "";
        public string description = "";

        protected override bool decode(BitStream stream)
        {
            captureName = BitOps.DecodeString(stream);
            description = BitOps.DecodeString(stream);

            return true;
        }

        protected override List<byte> encode()
        {
            List<byte> encoded = new List<byte>();

            BitOps.EncodeString(encoded, captureName);
            BitOps.EncodeString(encoded, description);

            return encoded;
        }
    }

    class RecordGame : Record
    {
        public GameRecord Record;
        public string comment = "";
        
        public void setRecord(GameRecord rec)
        {
            Record = rec;
            this.size = (uint)rec.getEstimatedSize();
        }

        public double getSecondsSinceStart(uint captureStart)
        {
            return Record.getRecordTime()/1000000.0;
        }

        public void setComment(string comment)
        {
            this.comment = comment;
        }

        public string getComment()
        {
            return comment;
        }

        protected override bool decode(BitStream stream)
        {
            Record = GameRecord.Factory.Decode(stream);

            if (Record == null)
                return false;

            return true;
        }

        protected override List<byte> encode()
        {
            List<byte> encoded = new List<byte>();

            encoded.AddRange(GameRecord.Factory.Encode(Record));

            return encoded;
        }
    }
}
