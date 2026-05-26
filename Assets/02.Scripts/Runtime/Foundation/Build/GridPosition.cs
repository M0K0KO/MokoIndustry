using Unity.Entities;
using Unity.Mathematics;

namespace MokoIndustry.Foundation.Build
{
    public struct GridPosition : IComponentData
    {
        public int2 Cell;
    }
}