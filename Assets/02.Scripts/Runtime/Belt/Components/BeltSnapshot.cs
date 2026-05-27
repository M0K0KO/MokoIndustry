using MokoIndustry.Foundation.Common;
using Unity.Collections;
using Unity.Entities;

namespace MokoIndustry.Belt
{
    public struct BeltSnapshot
    {
        public Entity Entity;
        public Direction4 Direction;
        public FixedList32Bytes<byte> Items;
        public FixedList32Bytes<sbyte> XOffsets;
        public FixedList32Bytes<byte> YPositions;
        public byte Length;

        public byte MinPosition;

        public static BeltSnapshot From(Entity entity, in BeltSegment belt)
        {
            return new BeltSnapshot
            {
                Entity = entity,
                Direction = belt.Direction,
                Items = belt.Items,
                XOffsets = belt.XOffsets,
                YPositions = belt.YPositions,
                Length = belt.Length,
                MinPosition = (belt.Length == 0) ? (byte)255 : belt.YPositions[0]
            };
        }
    }
}