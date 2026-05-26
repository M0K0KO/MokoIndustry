using Unity.Burst;
using Unity.Entities;

namespace MokoIndustry.Foundation.Tick
{
    [BurstCompile]
    [UpdateInGroup(typeof(FoundationSystemGroup))]
    public partial struct TickAdvanceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref var tick = ref SystemAPI.GetSingletonRW<TickSingleton>().ValueRW;
            tick.Current++;
        }
    }
}