using MokoIndustry.Belt;
using MokoIndustry.Foundation.Common;
using MokoIndustry.Foundation.Grid;
using MokoIndustry.Foundation.Input;
using MokoIndustry.Foundation.Tick;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace MokoIndustry.Foundation.Build
{
    [UpdateInGroup(typeof(CommandApplySystemGroup))]
    [BurstCompile]
    public partial struct BuildCommandSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickSingleton>();
            state.RequireForUpdate<GridConfigSingleton>();
            state.RequireForUpdate<GridOccupancySingleton>();
            state.RequireForUpdate<InputBufferSingleton>();
            state.RequireForUpdate<PrefabRegistrySingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tick = SystemAPI.GetSingleton<TickSingleton>();
            var config = SystemAPI.GetSingleton<GridConfigSingleton>();
            var occupancy = SystemAPI.GetSingleton<GridOccupancySingleton>();
            var registry = SystemAPI.GetSingleton<PrefabRegistrySingleton>();

            var bufferEntity = SystemAPI.GetSingletonEntity<InputBufferSingleton>();
            var buffer = SystemAPI.GetBuffer<InputBufferElement>(bufferEntity);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                        .CreateCommandBuffer(state.WorldUnmanaged);

            state.EntityManager.CompleteDependencyBeforeRW<BeltSegment>();
            var beltLookup = SystemAPI.GetComponentLookup<BeltSegment>(false);

            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                var cmd = buffer[i].Command;

                if (cmd.TargetTick > tick.Current) continue;
                buffer.RemoveAt(i);

                if (cmd.TargetTick < tick.Current)
                {
                    continue;
                }

                Apply(ecb, cmd, in config, occupancy, in registry, ref beltLookup);
            }
        }

        private void Apply(
            EntityCommandBuffer ecb,
            in InputCommand cmd,
            in GridConfigSingleton config,
            in GridOccupancySingleton occupancy,
            in PrefabRegistrySingleton registry, 
            ref ComponentLookup<BeltSegment> beltLookup)
        {
            if (!GridUtility.IsInBounds(cmd.Cell, in config)) return;

            switch (cmd.Type)
            {
                case CommandType.Build:
                    DoBuild(ecb, cmd, in occupancy, in config, in registry);
                    break;
                case CommandType.Demolish:
                    DoDemolish(ecb, cmd, in occupancy);
                    break;
                case CommandType.DebugInject:
                    DoDebugInject(cmd, occupancy, ref beltLookup);
                    break;
            }
        }

        private void DoBuild(
            EntityCommandBuffer ecb,
            in InputCommand cmd,
            in GridOccupancySingleton occupancy,
            in GridConfigSingleton gridConfig,
            in PrefabRegistrySingleton registry)
        {
            if (occupancy.Map.ContainsKey(cmd.Cell)) return;

            Entity prefab = cmd.Building switch
            {
                BuildingType.Dummy => registry.DummyBuildingPrefab,
                BuildingType.Belt => registry.BeltPrefab,
                _ => Entity.Null,
            };
            if (prefab == Entity.Null) return;

            occupancy.Map.Add(cmd.Cell, Entity.Null);

            var entity = ecb.Instantiate(prefab);
            ecb.SetComponent(entity, new GridPosition { Cell = cmd.Cell });
            ecb.SetComponent(entity, LocalTransform.FromPositionRotation(
                GridUtility.CellToWorld(cmd.Cell, gridConfig),
                quaternion.RotateY(cmd.Direction.ToRadians())));

            if (cmd.Building == BuildingType.Belt)
            {
                ecb.SetComponent(entity, BeltSegment.CreateEmpty(cmd.Direction));
            }
        }

        private void DoDemolish(
            EntityCommandBuffer ecb,
            in InputCommand cmd,
            in GridOccupancySingleton occupancy)
        {
            if (!occupancy.Map.TryGetValue(cmd.Cell, out var entity)) return;
            occupancy.Map.Remove(cmd.Cell);
            if (entity != Entity.Null)
                ecb.SetComponentEnabled<PendingDestroyTag>(entity, true);
        }

        private void DoDebugInject(
            in InputCommand cmd,
            GridOccupancySingleton occupancy,
            ref ComponentLookup<BeltSegment> beltLookup)
        {
            if (!occupancy.Map.TryGetValue(cmd.Cell, out var entity)) return;
            if (entity == Entity.Null) return;
            if (!beltLookup.HasComponent(entity)) return;

            ref var belt = ref beltLookup.GetRefRW(entity).ValueRW;

            if (belt.GetSlot(BeltConstants.SlotCount - 1) == ItemId.None)
                belt.SetItem(BeltConstants.SlotCount - 1, ItemId.Ore, 0);
        }
    }
}