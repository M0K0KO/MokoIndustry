using Unity.Entities;

namespace MokoIndustry.Foundation.Tick
{
    public struct TickSingleton : IComponentData
    {
        public int Current;
        public int InputDelay;
        public uint Seed;
    }
}
