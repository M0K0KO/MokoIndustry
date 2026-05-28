using MokoIndustry.Belt;
using MokoIndustry.Foundation;
using MokoIndustry.Foundation.Build;
using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(FoundationSystemGroup))]
public partial struct ItemRendererCleanupSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                            .CreateCommandBuffer(state.WorldUnmanaged);

        var beltLookup = SystemAPI.GetComponentLookup<BeltSegment>(true);

        foreach (var (renderRO, entity) in
                 SystemAPI.Query<RefRO<ItemRendererState>>().WithEntityAccess())
        {
            var owner = renderRO.ValueRO.OwnerBelt;
            if (owner == Entity.Null || !beltLookup.HasComponent(owner))
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}