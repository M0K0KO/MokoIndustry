using Unity.Entities;

namespace MokoIndustry.Foundation.Determinism
{
    public struct DeterminismHashSingleton : IComponentData
    {
        public ulong CurrentHash;
        public int LastCheckedTick;
        public int CheckInterval;
    }
}