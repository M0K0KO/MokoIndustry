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
        public FixedList32Bytes<byte> SlotProgress;

        public static BeltSnapshot From(Entity entity, in BeltSegment belt)
        {
            return new BeltSnapshot
            {
                Entity = entity,
                Direction = belt.Direction,
                Slots = belt.Slots,
                SlotProgress = belt.SlotProgress
            };
        }

        public ItemId GetSlot(int index)
        {
            return (ItemId)Slots[index];
        }

        public byte GetProgress(int index)
        {
            return SlotProgress[index];
        }
    }
}