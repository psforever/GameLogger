using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCap
{

    
    class BitStream : object
    {
        public byte [] data { get; private set; }
        int nextByte;

        public BitStream()
        {
            data = new byte[0];
            nextByte = 0;
        }

        public BitStream(List<byte> data)
        {
            this.data = data.ToArray();
            nextByte = 0;
        }

        public BitStream(byte [] data)
        {
            this.data = data;
            nextByte = 0;
        }

        public void append(byte [] other)
        {
            data = data.Concat(other);
        }

        public void append(byte[] other, int offset, int size)
        {
            data = data.ConcatSome(other, offset, size);
        }

        public void append(List<byte> data)
        {
            append(data.ToArray());
        }

        public bool isEnd()
        {
            return nextByte == size();
        }

        public int position()
        {
            return nextByte;
        }

        public int size()
        {
            return data.Length;
        }

        public int sizeLeft()
        {
            return size() - nextByte;
        }

        public void seek(int pos)
        {
            if (pos < 0 || pos >= data.Length)
                throw new ArgumentException("seek position out of bounds");

            nextByte = pos;
        }

        public void seekEnd()
        {
            nextByte = size();
        }

        public void rewind()
        {
            nextByte = 0;
        }

        public bool advance(int amount)
        {
            if (amount + nextByte > size())
                return false;

            nextByte += amount;

            return true;
        }

        /*public List<byte> copyRest()
        {
            if (sizeLeft() == 0)
                return new List<byte>();

            byte[] newCopy = new byte[sizeLeft()];

            Array.ConstrainedCopy(data, position(), newCopy, 0, sizeLeft());

            return new List<byte>(newCopy);
        }*/

        public string extractString(uint size)
        {
            if (size > sizeLeft())
                return null;

            string retString = Encoding.ASCII.GetString(data, position(), (int)size);
            advance((int)size);

            return retString;
        }

        public List<byte> extractOctetStream(uint size)
        {
            if (size > sizeLeft())
                return null;

            byte[] newCopy = new byte[size];

            Array.ConstrainedCopy(data, position(), newCopy, 0, (int)size);
            advance((int)size);

            return new List<byte>(newCopy);
        }
    }

    static class BitOps
    {
        enum EncodingFlags
        {
            // bottom 6 bits used for type field
            ENCODING_TYPE_STRING = 0x1,
            ENCODING_TYPE_OCTET_STREAM = 0x2,

            // DO NOT EXCEED THIS NUMBER FOR ENCODING TYPE
            ENCODING_TYPE_MAX = 0x3F,
            // top two bits used to signal length modifiers
            // bit 6 = uint16_t
            // bit 7 = uint32_T
            // These bits are exclusive - only one or none may be set.
            // If none are set, then the following length field is a uint8_t
            ENCODING_LENGTH_WORD = 0x40,
            ENCODING_LENGTH_DWORD = 0x80
        }

        public static bool ReadBool(BitStream stream)
        {
            return Convert.ToBoolean(ReadByte(stream));
        }

        public static byte ReadByte(BitStream stream)
        {
            if(stream.isEnd())
            {
                throw new InvalidOperationException("Reached end of bitstream");
            }

            int pos = stream.position();

            if (!stream.advance(sizeof(byte)))
                throw new InvalidOperationException("Data requested exceeds bitstream");

            byte data = stream.data[pos];

            return data;
        }

        public static void WriteByte(List<byte> buf, byte val)
        {
            buf.Add(val);
        }

        public static ushort ReadUInt16(BitStream stream)
        {
            if (stream.isEnd())
            {
                throw new InvalidOperationException("Reached end of bitstream");
            }

            int pos = stream.position();

            if (!stream.advance(sizeof(ushort)))
                throw new InvalidOperationException("Data requested exceeds bitstream");

            ushort data = BitConverter.ToUInt16(stream.data, pos);

            return data;
        }

        public static void WriteUInt16(List<byte> buf, ushort val)
        {
            buf.AddRange(BitConverter.GetBytes(val));
        }

        public static uint ReadUInt32(BitStream stream)
        {
            if (stream.isEnd())
            {
                throw new InvalidOperationException("Reached end of bitstream");
            }

            int pos = stream.position();

            if (!stream.advance(sizeof(uint)))
                throw new InvalidOperationException("Data requested exceeds bitstream");

            uint data = BitConverter.ToUInt32(stream.data, pos);

            return data;
        }

        public static void WriteUInt32(List<byte> buf, uint val)
        {
            buf.AddRange(BitConverter.GetBytes(val));
        }

        public static ulong ReadUInt64(BitStream stream)
        {
            if (stream.isEnd())
            {
                throw new InvalidOperationException("Reached end of bitstream");
            }

            int pos = stream.position();

            if (!stream.advance(sizeof(ulong)))
                throw new InvalidOperationException("Data requested exceeds bitstream");

            ulong data = BitConverter.ToUInt64(stream.data, pos);

            return data;
        }

        public static void WriteUInt64(List<byte> buf, ulong val)
        {
            buf.AddRange(BitConverter.GetBytes(val));
        }

        private static void EncodeTypeLength(List<byte> buf, EncodingFlags type, uint length)
        {
            byte encodingType = (byte)type;

            if (length > UInt16.MaxValue && length <= UInt32.MaxValue)
            {
                encodingType |= (byte)EncodingFlags.ENCODING_LENGTH_DWORD;

                buf.Add(encodingType);
                WriteUInt32(buf, length);
            }
            else if (length > Byte.MaxValue && length <= UInt16.MaxValue)
            {
                encodingType |= (byte)EncodingFlags.ENCODING_LENGTH_WORD;

                buf.Add(encodingType);
                WriteUInt16(buf, (ushort)length);
            }
            else
            {
                buf.Add(encodingType);
                WriteByte(buf, (byte)length);
            }
        }

        private static uint DecodeTypeLength(BitStream stream, EncodingFlags expectedType)
        {
            byte encodingType = ReadByte(stream);

            if ((encodingType & (byte)expectedType) == 0)
                throw new InvalidOperationException("Invalid data encoding type");

            // remove string encoding type
            // C# was pretty annoying here...had to use minus. Still works
            encodingType -= (byte)expectedType;

            // make sure either no bits are set or only one
            if ((encodingType & encodingType - 1) != 0)
                throw new InvalidOperationException("Multiple data lengths specified");

            uint size = 0;

            if ((encodingType & (byte)EncodingFlags.ENCODING_LENGTH_DWORD) != 0)
                size = ReadUInt32(stream);
            else if ((encodingType & (byte)EncodingFlags.ENCODING_LENGTH_WORD) != 0)
                size = ReadUInt16(stream);
            else
                size = ReadByte(stream);

            return size;
        }

        public static void EncodeString(List<byte> buf, string val)
        {
            EncodeTypeLength(buf, EncodingFlags.ENCODING_TYPE_STRING, (uint)val.Length);
            buf.AddRange(Encoding.ASCII.GetBytes(val));
        }

        public static string DecodeString(BitStream stream)
        {
            uint strSize = DecodeTypeLength(stream, EncodingFlags.ENCODING_TYPE_STRING);
            string outStr = stream.extractString(strSize);

            if (outStr == null)
                throw new InvalidOperationException("String decoding exceeds bitstream");

            return outStr;
        }

        public static void EncodeOctetStream(List<byte> buf, List<byte> val)
        {
            EncodeTypeLength(buf, EncodingFlags.ENCODING_TYPE_OCTET_STREAM, (uint)val.Count);
            buf.AddRange(val.AsEnumerable());
        }

        public static List<byte> DecodeOctetStream(BitStream stream)
        {
            uint size = DecodeTypeLength(stream, EncodingFlags.ENCODING_TYPE_OCTET_STREAM);
            List<byte> outStream = stream.extractOctetStream(size);

            if (outStream == null)
                throw new InvalidOperationException("Octet stream decoding exceeds bitstream");

            return outStream;
        }
    }
}
