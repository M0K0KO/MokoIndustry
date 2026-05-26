using Unity.Entities;
using Unity.Mathematics;

namespace MokoIndustry.Foundation.Interpolation
{
    // would be attached to entity who needs interpolation
    // render system will lerp every frame
    public struct InterpolationState : IComponentData
    {
        public float3 Previous;  // 직전 Tick 끝의 위치
        public float3 Current;   // 현재 Tick 끝의 위치
    }
}