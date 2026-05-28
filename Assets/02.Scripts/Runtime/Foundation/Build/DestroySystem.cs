using MokoIndustry.Foundation.Common;
using MokoIndustry.Foundation.Grid;
using MokoIndustry.Foundation.Input;
using MokoIndustry.Logistics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MokoIndustry.Foundation.Build
{
    [UpdateInGroup(typeof(CommandApplySystemGroup))]
    [UpdateAfter(typeof(GridOccupancyRegisterSystem))]
    [UpdateAfter(typeof(ConnectionSystem))]
    public partial struct DestroySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridOccupancySingleton>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var occ = SystemAPI.GetSingleton<GridOccupancySingleton>();

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                        .CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new DestroyJob
            {
                Occupancy = occ.Map,
                ECB = ecb
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(PendingDestroyTag))]
        private partial struct DestroyJob : IJobEntity
        {
            public NativeParallelHashMap<int2, Entity> Occupancy;
            public EntityCommandBuffer ECB;

            private void Execute(in GridPosition gridPos, Entity entity)
            {
                Occupancy.Remove(gridPos.Cell);
                ECB.DestroyEntity(entity);
            }
        }
    }
}