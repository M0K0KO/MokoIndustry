using MokoIndustry.Belt;
using MokoIndustry.Foundation.Common;
using MokoIndustry.Foundation.Grid;
using MokoIndustry.Foundation.Input;
using MokoIndustry.Foundation.Tick;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace MokoIndustry.Foundation.Build
{
    [UpdateInGroup(typeof(CommandApplySystemGroup))]
    public partial struct BuildCommandSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickSingleton>();
            state.RequireForUpdate<GridConfigSingleton>();
            state.RequireForUpdate<GridOccupancySingleton>();
            state.RequireForUpdate<InputBufferSingleton>();
            state.RequireForUpdate<PrefabRegistrySingleton>();
        }

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

            if (buffer.Length > 0)
            {
                UnityEngine.Debug.Log($"[Build] OnUpdate: Tick={tick.Current}, " +
                                      $"BufferLen={buffer.Length}, " +
                                      $"Frame={UnityEngine.Time.frameCount}");
                for (int i = 0; i < buffer.Length; i++)
                {
                    UnityEngine.Debug.Log($"  [{i}] TargetTick={buffer[i].Command.TargetTick}");
                }
            }

            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                var cmd = buffer[i].Command;

                if (cmd.TargetTick > tick.Current) continue;
                buffer.RemoveAt(i);

                if (cmd.TargetTick < tick.Current)
                {
                    Debug.LogWarning(
                        $"[Build] Stale: TargetTick={cmd.TargetTick}, Current={tick.Current}");
                    continue;
                }

                Apply(ecb, cmd, in config, occupancy, in registry);
            }
        }

        private void Apply(
            EntityCommandBuffer ecb,
            in InputCommand cmd,
            in GridConfigSingleton config,
            in GridOccupancySingleton occupancy,
            in PrefabRegistrySingleton registry)
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
                ecb.SetComponent(entity, new BeltSegment
                {
                    Direction = cmd.Direction,
                    Slots = default,
                    Progress = 0f,
                });
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
    }
}