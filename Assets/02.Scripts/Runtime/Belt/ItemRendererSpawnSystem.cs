using MokoIndustry.Foundation;
using MokoIndustry.Foundation.Build;
using Unity.Entities;

namespace MokoIndustry.Belt
{
    [UpdateInGroup(typeof(FoundationSystemGroup))]
    [UpdateAfter(typeof(GridOccupancyRegisterSystem))]
    public partial struct ItemRendererSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabRegistrySingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var registry = SystemAPI.GetSingleton<PrefabRegistrySingleton>();
            if (registry.ItemRendererPrefab == Entity.Null) return;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                                      .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_, beltEntity) in SystemAPI.Query<RefRO<BeltTag>>().WithAll<ItemRendererNeedsSpawnTag>().WithEntityAccess())
            {
                for (int i = 0; i < BeltConstants.SlotCount; i++)
                {
                    var child = ecb.Instantiate(registry.ItemRendererPrefab);
                    ecb.SetComponent(child, new ItemRendererState
                    {
                        OwnerBelt = beltEntity,
                        SlotIndex = i,
                    });
                }

                ecb.SetComponentEnabled<ItemRendererNeedsSpawnTag>(beltEntity, false);
            }
        }
    }
}