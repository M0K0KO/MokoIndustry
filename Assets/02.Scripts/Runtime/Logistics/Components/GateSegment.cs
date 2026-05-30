using MokoIndustry.Foundation.Common;
using Unity.Collections;
using Unity.Entities;

namespace MokoIndustry.Logistics
{
    public enum GateMode : byte
    {
        Overflow = 0, Underflow = 1
    }

    public struct GateSegment : IComponentData
    {
        public const int Capacity = 4;

        public FixedList32Bytes<byte> Buffer;
        public byte OutputCooldown;
        public GateMode Mode;
        public Direction4 Direction;
        public byte LastSideUsed;
    }
}