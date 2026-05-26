using Unity.Entities;
using Unity.Mathematics;

namespace MokoIndustry.Foundation.Grid
{
    public struct GridConfigSingleton : IComponentData
    {
        public int2 Size;
        public float CellSize;
        public float3 Origin;
    }
}