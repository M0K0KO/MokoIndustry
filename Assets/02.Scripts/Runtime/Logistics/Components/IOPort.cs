using Unity.Entities;

namespace MokoIndustry.Logistics
{
    public struct IOPort : IComponentData
    {
        // DirectionBit OR
        public byte InputMask;

        // DirectionBit OR
        public byte OutputMask;

        // 0 = anything, else = only that ItemId
        public byte AcceptFilter;
    }
}