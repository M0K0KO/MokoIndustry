using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace MokoIndustry.Foundation.Interpolation
{
    /// <summary>
    /// 매 프레임 alpha를 계산해서 InterpolationState를 LocalTransform에 반영.
    /// PresentationSystemGroup에 들어가서 시뮬레이션 끝난 후 시각화.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct TransformInterpolationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimingSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timing = SystemAPI.GetSingleton<TickTimingSingleton>();

            double elapsedSinceTick = SystemAPI.Time.ElapsedTime - timing.LastTickElapsedTime;
            float alpha = (float)math.saturate(elapsedSinceTick / timing.TickDuration);

            state.Dependency = new InterpolationJob
            {
                Alpha = alpha,
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct InterpolationJob : IJobEntity
    {
        public float Alpha;

        public void Execute(in InterpolationState state, ref LocalTransform transform)
        {
            transform.Position = math.lerp(state.Previous, state.Current, Alpha);
        }
    }
}