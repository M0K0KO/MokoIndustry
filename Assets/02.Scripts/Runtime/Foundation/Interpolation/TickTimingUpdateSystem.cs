using MokoIndustry.Foundation.Tick;
using Unity.Burst;
using Unity.Entities;

namespace MokoIndustry.Foundation.Interpolation
{
    /// <summary>
    /// FixedStep 끝에서 "지금이 마지막 Tick 종료 시각"으로 기록
    /// 다음 frame에서 interpolate 시에 기준점으로 사용
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FoundationSystemGroup), OrderLast = true)]
    public partial struct TickTimingUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickSingleton>();

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new TickTimingSingleton
            {
                LastTickElapsedTime = 0,
                TickDuration = FoundationConstants.TickDuration
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref var timing = ref SystemAPI.GetSingletonRW<TickTimingSingleton>().ValueRW;
            timing.LastTickElapsedTime = SystemAPI.Time.ElapsedTime;
        }
    }
}