using Unity.Entities;

namespace MokoIndustry.Foundation.Interpolation
{
    public struct TickTimingSingleton : IComponentData
    {
        public double LastTickElapsedTime;
        public double TickDuration;
    }
}