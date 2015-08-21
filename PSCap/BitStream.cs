using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCap
{
    class BitStream : object
    {
        public byte [] data { get; }
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

        public void seekEnd()
        {
            nextByte = size();
        }

        public void seekStart()
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
    }

    static class BitOps
    {
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
    }
}
