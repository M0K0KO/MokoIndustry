using MokoIndustry.Foundation.Build;
using MokoIndustry.Foundation.Common;
using MokoIndustry.Foundation.Grid;
using MokoIndustry.Foundation.Interpolation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace MokoIndustry.Belt
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    public partial struct ItemRendererUpdateSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridConfigSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timing = SystemAPI.GetSingleton<TickTimingSingleton>();
            var config = SystemAPI.GetSingleton<GridConfigSingleton>();
            var beltLookup = SystemAPI.GetComponentLookup<BeltSegment>(true);
            var gridLookup = SystemAPI.GetComponentLookup<GridPosition>(true);

            double elapsedSinceTick = SystemAPI.Time.ElapsedTime - timing.LastTickElapsedTime;
            float alpha = (float)math.saturate(elapsedSinceTick / timing.TickDuration);

            state.Dependency = new UpdateJob
            {
                Alpha = alpha,
                Config = config,
                BeltLookup = beltLookup,
                GridLookup = gridLookup,
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct UpdateJob : IJobEntity
        {
            public float Alpha;
            [ReadOnly] public GridConfigSingleton Config;
            [ReadOnly] public ComponentLookup<BeltSegment> BeltLookup;
            [ReadOnly] public ComponentLookup<GridPosition> GridLookup;

            void Execute(
                in ItemRendererState state,
                ref LocalTransform transform,
                ref URPMaterialPropertyBaseColor color)
            {
                var owner = state.OwnerBelt;
                if (owner == Entity.Null || !BeltLookup.HasComponent(owner) || !GridLookup.HasComponent(owner))
                {
                    transform.Position = new float3(-9999f, -9999f, -9999f);
                    return;
                }

                var belt = BeltLookup[owner];
                var gridPos = GridLookup[owner];

                int i = state.SlotIndex;
                var item = belt.GetSlot(i);
                var progress = belt.GetProgress(i);

                if (item == ItemId.None)
                {
                    color.Value = new float4(0f, 0f, 0f, 0f);
                    transform.Position = new float3(-9999f, -9999f, -9999f);
                    return;
                }

                float currentProgress = progress;
                float visualProgress = math.min(
                    currentProgress + Alpha * BeltConstants.SpeedPerTickByte,
                    (float)BeltConstants.MaxProgress);

                float t = visualProgress / (float)BeltConstants.MaxProgress;
                float along = (1.5f - i + t) * BeltConstants.SlotSize;

                float2 dirVec = (float2)belt.Direction.ToOffset();

                float3 beltWorldPos = GridUtility.CellToWorld(gridPos.Cell, Config);

                float3 slotWorldPos = beltWorldPos + new float3(
                    dirVec.x * along,
                    0f,
                    dirVec.y * along
                );

                slotWorldPos.y += 0.3f;
                transform.Position = slotWorldPos;
                transform.Scale = 1f;
                transform.Rotation = quaternion.identity;

                color.Value = ItemColor(item);
            }

            private static float4 ItemColor(ItemId item)
            {
                return item switch
                {
                    ItemId.Ore => new float4(0.65f, 0.40f, 0.15f, 1f),  // 갈색
                    ItemId.Plate => new float4(0.75f, 0.75f, 0.80f, 1f),  // 회색
                    ItemId.Ammo => new float4(0.95f, 0.85f, 0.20f, 1f),  // 노랑
                    _ => new float4(1f, 0f, 1f, 1f),
                };
            }
        }
    }
}