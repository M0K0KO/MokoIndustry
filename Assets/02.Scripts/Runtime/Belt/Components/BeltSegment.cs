using MokoIndustry.Foundation.Common;
using Unity.Collections;
using Unity.Entities;

namespace MokoIndustry.Belt
{
    public struct BeltSegment : IComponentData
    {
        public Direction4 Direction;

        public FixedList32Bytes<byte> Items;       // ItemId[capacity], capacity=3

        public FixedList32Bytes<sbyte> XOffsets;   // int8, [-127, 127]

        public FixedList32Bytes<byte> YPositions;  // uint8, [0, 255]

        public byte Length;

        public FixedList32Bytes<byte> PrevItems;

        public FixedList32Bytes<byte> PrevYPositions;

        public FixedList32Bytes<sbyte> PrevXOffsets;

        public byte PrevLength;

        public byte InsertedAtTailThisTick;

        public void InsertAtTail(ItemId item, sbyte xOffset, byte startY)
        {
            for (int i = Length; i > 0; i--)
            {
                Items[i] = Items[i - 1];
                XOffsets[i] = XOffsets[i - 1];
                YPositions[i] = YPositions[i - 1];
            }
            Items[0] = (byte)item;
            XOffsets[0] = xOffset;
            YPositions[0] = startY;
            Length++;
        }

        public void RemoveHead()
        {
            if (Length > 0) Length--;
        }

        public ItemId GetItem(int i) => (ItemId)Items[i];
        public byte GetY(int i) => YPositions[i];
        public sbyte GetX(int i) => XOffsets[i];

        public static BeltSegment CreateEmpty(Direction4 dir)
        {
            var items = new FixedList32Bytes<byte>(); items.Length = BeltConstants.Capacity;
            var xs = new FixedList32Bytes<sbyte>(); xs.Length = BeltConstants.Capacity;
            var ys = new FixedList32Bytes<byte>(); ys.Length = BeltConstants.Capacity;
            var pxs = new FixedList32Bytes<sbyte>(); pxs.Length = BeltConstants.Capacity;
            var pys = new FixedList32Bytes<byte>(); pys.Length = BeltConstants.Capacity;

            return new BeltSegment { 
                Direction = dir, Items = items, 
                XOffsets = xs, YPositions = ys, Length = 0,
                PrevItems = items, PrevXOffsets = pxs, PrevYPositions = pys, PrevLength = 0,

                InsertedAtTailThisTick = 0
            };
        }
    }
}