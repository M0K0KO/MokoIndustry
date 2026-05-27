using MokoIndustry.Foundation.Common;
using Unity.Collections;
using Unity.Entities;

namespace MokoIndustry.Belt
{
    public struct BeltSnapshot
    {
        public Entity Entity;
        public Direction4 Direction;
        public FixedList32Bytes<byte> Slots;
        public float Progress;
    }
}