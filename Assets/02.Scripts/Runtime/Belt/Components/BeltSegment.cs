using MokoIndustry.Foundation.Common;
using Unity.Collections;
using Unity.Entities;

namespace MokoIndustry.Belt
{
    public struct BeltSegment : IComponentData
    {
        public Direction4 Direction;

        public FixedList32Bytes<byte> Slots;
        public FixedList32Bytes<byte> SlotProgress;

        public float Progress;

        public const int SlotCount = BeltConstants.SlotCount;

        public ItemId GetSlot(int index) => (ItemId)Slots[index];
        public void SetSlot(int index, ItemId item) => Slots[index] = (byte)item;
        public byte GetProgress(int index)
        {
            return SlotProgress[index];
        }
        public void SetProgress(int index, byte progress)
        {
            SlotProgress[index] = progress;
        }
        public void ClearSlot(int index)
        {
            Slots[index] = (byte)ItemId.None;
            SlotProgress[index] = 0;
        }
        public void SetItem(int index, ItemId item, byte progress = 0)
        {
            Slots[index] = (byte)item;
            SlotProgress[index] = progress;
        }

        public static BeltSegment CreateEmpty(Direction4 direction)
        {
            var slots = new FixedList32Bytes<byte>();
            var progress = new FixedList32Bytes<byte>();

            for (int i = 0; i < SlotCount; i++)
            {
                slots.Add((byte)ItemId.None);
                progress.Add(0);
            }

            return new BeltSegment
            {
                Direction = direction,
                Slots = slots,
                SlotProgress = progress
            };
        }
    }
}