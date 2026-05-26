using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MokoIndustry.Foundation.Grid
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct GridOccupancyInitSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridConfigSingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<GridOccupancySingleton>())
            {
                state.Enabled = false;
                return;
            }

            var config = SystemAPI.GetSingleton<GridConfigSingleton>();
            int capacity = config.Size.x * config.Size.y;

            var map = new NativeHashMap<int2, Entity>(capacity, Allocator.Persistent);

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new GridOccupancySingleton
            {
                Map = map
            });

            state.Enabled = false;
        }

        public void OnDestroy(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<GridOccupancySingleton>())
            {
                var occupancy = SystemAPI.GetSingleton<GridOccupancySingleton>();
                if (occupancy.Map.IsCreated)
                {
                    occupancy.Map.Dispose();
                }
            }
        }
    }
}