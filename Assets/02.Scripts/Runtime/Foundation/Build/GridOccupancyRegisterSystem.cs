using MokoIndustry.Foundation.Grid;
using MokoIndustry.Foundation.Input;
using Unity.Entities;

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

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                            .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (gridPosRO, entity) in SystemAPI.Query<RefRO<GridPosition>>().WithAll<NewlyBuiltTag>().WithEntityAccess())
            {
                var cell = gridPosRO.ValueRO.Cell;

                if (occupancy.Map.TryGetValue(cell, out var existing) && existing == Entity.Null)
                {
                    occupancy.Map[cell] = entity;
                }
                else if (!occupancy.Map.ContainsKey(cell))
                {
                    occupancy.Map.Add(cell, entity);
                }

                ecb.SetComponentEnabled<NewlyBuiltTag>(entity, false);
            }
        }
    }
}