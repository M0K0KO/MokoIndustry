using MokoIndustry.Foundation;
using Unity.Collections;
using Unity.Entities;

namespace MokoIndustry.Belt
{
    public struct BeltSegment : IComponentData
    {
        public Direction4 Direction;

        public FixedList32Bytes<byte> Slots;

        public float Progress;

        public const int SlotCount = 4;

        public ItemId GetSlot(int index) => (ItemId)Slots[index];
        public void SetSlot(int index, ItemId item) => Slots[index] = (byte)item;
    }
}