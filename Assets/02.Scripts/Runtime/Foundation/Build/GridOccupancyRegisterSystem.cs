using MokoIndustry.Foundation.Grid;
using MokoIndustry.Foundation.Input;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MokoIndustry.Foundation.Build
{
    [UpdateInGroup(typeof(FoundationSystemGroup))]
    [UpdateBefore(typeof(CommandApplySystemGroup))]
    public partial struct GridOccupancyRegisterSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridOccupancySingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var occupancy = SystemAPI.GetSingleton<GridOccupancySingleton>();

            state.Dependency = new RegisterOccupancyJob
            {
                Occupancy = occupancy.Map,
            }.Schedule(state.Dependency);

            state.Dependency.Complete();
        }

        [BurstCompile]
        [WithAll(typeof(NewlyBuiltTag))]
        private partial struct RegisterOccupancyJob : IJobEntity
        {
            public NativeParallelHashMap<int2, Entity> Occupancy;

            private void Execute(in GridPosition gridPos, Entity entity)
            {
                var cell = gridPos.Cell;

                if (Occupancy.TryGetValue(cell, out var existing))
                {
                    if (existing == Entity.Null)
                    {
                        Occupancy[cell] = entity;
                    }
                }
                else
                {
                    Occupancy.Add(cell, entity);
                }
            }
        }
    }
}