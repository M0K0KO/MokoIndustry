using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MokoIndustry.Foundation.Grid
{
    public struct GridOccupancySingleton : IComponentData
    {
        public NativeHashMap<int2, Entity> Map;
    }
}