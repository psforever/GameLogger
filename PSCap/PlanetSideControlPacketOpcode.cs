using System;
using System.Collections.Generic;

namespace PSCap
{

    public sealed class PlanetSideControlPacketOpcode
    {
        private readonly string name;
        private readonly byte opcode;
        private static readonly Dictionary<byte, PlanetSideControlPacketOpcode> instance = new Dictionary<byte, PlanetSideControlPacketOpcode>();

        public static readonly PlanetSideControlPacketOpcode HandleGamePacket = new PlanetSideControlPacketOpcode(0, "HandleGamePacket");
        public static readonly PlanetSideControlPacketOpcode ClientStart = new PlanetSideControlPacketOpcode(1, "ClientStart");
        public static readonly PlanetSideControlPacketOpcode ServerStart = new PlanetSideControlPacketOpcode(2, "ServerStart");
        public static readonly PlanetSideControlPacketOpcode MultiPacket = new PlanetSideControlPacketOpcode(3, "MultiPacket");
        public static readonly PlanetSideControlPacketOpcode UnknownMessage4 = new PlanetSideControlPacketOpcode(4, "UnknownMessage4");
        public static readonly PlanetSideControlPacketOpcode UnknownMessage5 = new PlanetSideControlPacketOpcode(5, "UnknownMessage5");
        public static readonly PlanetSideControlPacketOpcode UnknownMessage6 = new PlanetSideControlPacketOpcode(6, "UnknownMessage6");
        public static readonly PlanetSideControlPacketOpcode UnknownMessage7 = new PlanetSideControlPacketOpcode(7, "UnknownMessage7");
        public static readonly PlanetSideControlPacketOpcode UnknownMessage8 = new PlanetSideControlPacketOpcode(8, "UnknownMessage8");
        public static readonly PlanetSideControlPacketOpcode SlottedMetaPacket0 = new PlanetSideControlPacketOpcode(9, "SlottedMetaPacket0");
        public static readonly PlanetSideControlPacketOpcode SlottedMetaPacket1 = new PlanetSideControlPacketOpcode(10, "SlottedMetaPacket1");
        public static readonly PlanetSideControlPacketOpcode SlottedMetaPacket2 = new PlanetSideControlPacketOpcode(11, "SlottedMetaPacket2");
        public static readonly PlanetSideControlPacketOpcode SlottedMetaPacket3 = new PlanetSideControlPacketOpcode(12, "SlottedMetaPacket3");
        public static readonly PlanetSideControlPacketOpcode SlottedMetaPacket4 = new PlanetSideControlPacketOpcode(13, "SlottedMetaPacket4");
        public static readonly PlanetSideControlPacketOpcode SlottedMetaPacket5 = new PlanetSideControlPacketOpcode(14, "SlottedMetaPacket5");
        public static readonly PlanetSideControlPacketOpcode SlottedMetaPacket6 = new PlanetSideControlPacketOpcode(15, "SlottedMetaPacket6");
        public static readonly PlanetSideControlPacketOpcode SlottedMetaPacket7 = new PlanetSideControlPacketOpcode(16, "SlottedMetaPacket7");
        public static readonly PlanetSideControlPacketOpcode RelatedA0 = new PlanetSideControlPacketOpcode(17, "RelatedA0");
        public static readonly PlanetSideControlPacketOpcode RelatedA1 = new PlanetSideControlPacketOpcode(18, "RelatedA1");
        public static readonly PlanetSideControlPacketOpcode RelatedA2 = new PlanetSideControlPacketOpcode(19, "RelatedA2");
        public static readonly PlanetSideControlPacketOpcode RelatedA3 = new PlanetSideControlPacketOpcode(20, "RelatedA3");
        public static readonly PlanetSideControlPacketOpcode RelatedB0 = new PlanetSideControlPacketOpcode(21, "RelatedB0");
        public static readonly PlanetSideControlPacketOpcode RelatedB1 = new PlanetSideControlPacketOpcode(22, "RelatedB1");
        public static readonly PlanetSideControlPacketOpcode RelatedB2 = new PlanetSideControlPacketOpcode(23, "RelatedB2");
        public static readonly PlanetSideControlPacketOpcode RelatedB3 = new PlanetSideControlPacketOpcode(24, "RelatedB3");
        public static readonly PlanetSideControlPacketOpcode AggregatePacket = new PlanetSideControlPacketOpcode(25, "AggregatePacket");
        public static readonly PlanetSideControlPacketOpcode UnknownMessage26 = new PlanetSideControlPacketOpcode(26, "UnknownMessage26");
        public static readonly PlanetSideControlPacketOpcode UnknownMessage27 = new PlanetSideControlPacketOpcode(27, "UnknownMessage27");
        public static readonly PlanetSideControlPacketOpcode UnknownMessage28 = new PlanetSideControlPacketOpcode(28, "UnknownMessage28");
        public static readonly PlanetSideControlPacketOpcode ConnectionClose = new PlanetSideControlPacketOpcode(29, "ConnectionClose");
        public static readonly PlanetSideControlPacketOpcode UnknownMessage30 = new PlanetSideControlPacketOpcode(30, "UnknownMessage30");

        private PlanetSideControlPacketOpcode(byte opcode, string name)
        {
            this.opcode = opcode;
            this.name = name;
            instance[opcode] = this;
        }

        public override string ToString()
        {
            return name;
        }

        public static explicit operator PlanetSideControlPacketOpcode(byte opcode)
        {
            PlanetSideControlPacketOpcode res;

            if (instance.TryGetValue(opcode, out res))
                return res;
            else
		        return new PlanetSideControlPacketOpcode(opcode, "UnknownMessage" + opcode.ToString());
        }
    }
}
