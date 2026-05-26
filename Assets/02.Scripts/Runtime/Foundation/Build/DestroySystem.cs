using MokoIndustry.Foundation.Common;
using MokoIndustry.Foundation.Input;
using Unity.Entities;

namespace MokoIndustry.Foundation.Build
{
    [UpdateInGroup(typeof(FoundationSystemGroup))]
    [UpdateBefore(typeof(CommandApplySystemGroup))]
    [UpdateAfter(typeof(GridOccupancyRegisterSystem))]
    public partial struct DestroySystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                        .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_, entity) in
                     SystemAPI.Query<RefRO<PendingDestroyTag>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}