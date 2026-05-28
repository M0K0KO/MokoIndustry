using MokoIndustry.Foundation.Common;
using Unity.Collections;
using Unity.Entities;

namespace MokoIndustry.Belt
{
    public struct BeltSnapshot
    {
        public Entity Entity;

        // public abstract fields (IntentJob will look at these)
        public byte Kind;
        public ItemId HeadItem;
        public bool ReadyToOutput;
        public bool CanAcceptIn;

        // dedicated fields for belt (ApplyJob will look at these)
        public Direction4 Direction;
        public FixedList32Bytes<byte> Items;
        public FixedList32Bytes<sbyte> XOffsets;
        public FixedList32Bytes<byte> YPositions;
        public byte HeadY;
        public byte Length;
        public byte MinPosition;

        public const byte KindBelt = 0;
        public const byte KindRouter = 1;

        public static BeltSnapshot From(Entity entity, in BeltSegment belt)
        {
            byte len = belt.Length;
            byte minPos = (len == 0) ? (byte)255 : belt.YPositions[0];

            return new BeltSnapshot
            {
                Entity = entity,
                Kind = KindBelt,

                // public fields
                HeadItem = (len == 0) ? ItemId.None : (ItemId)belt.Items[len - 1],
                ReadyToOutput = len > 0 &&
                                belt.YPositions[len - 1] + BeltConstants.SpeedPerTick
                                    >= BeltConstants.MaxPosition,
                CanAcceptIn = len < BeltConstants.Capacity &&
                                minPos >= BeltConstants.ItemSpace,

                // belt fields
                Direction = belt.Direction,
                Items = belt.Items,
                XOffsets = belt.XOffsets,
                YPositions = belt.YPositions,
                HeadY = (len == 0) ? (byte)0 : belt.YPositions[len - 1],
                Length = len,
                MinPosition = minPos
            };
        }
    }
}