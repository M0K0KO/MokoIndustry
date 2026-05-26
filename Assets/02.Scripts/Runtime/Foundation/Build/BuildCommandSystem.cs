using MokoIndustry.Foundation.Grid;
using MokoIndustry.Foundation.Input;
using MokoIndustry.Foundation.Tick;
using Unity.Collections;
using Unity.Entities;
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

            var pending = new NativeList<InputCommand>(buffer.Length, Allocator.Temp);

            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                var cmd = buffer[i].Command;

                if (cmd.TargetTick > tick.Current)
                    continue;

                buffer.RemoveAt(i);

                if (cmd.TargetTick < tick.Current)
                {
                    Debug.LogWarning(
                        $"[Build] Stale command: TargetTick={cmd.TargetTick}, Current={tick.Current}");
                    continue;
                }

                pending.Add(cmd);
            }

            for (int i = pending.Length - 1; i >= 0; i--)
            {
                var cmd = pending[i];
                Apply(ref state, cmd, in config, in occupancy, in registry);
            }

            pending.Dispose();
        }

        private void Apply(
            ref SystemState state,
            in InputCommand cmd,
            in GridConfigSingleton config,
            in GridOccupancySingleton occupancy,
            in PrefabRegistrySingleton registry)
        {
            if (!GridUtility.IsInBounds(cmd.Cell, in config)) return;

            switch (cmd.Type)
            {
                case CommandType.Build:
                    DoBuild(ref state, cmd, in occupancy, in config, registry.DummyBuildingPrefab);
                    break;
                case CommandType.Demolish:
                    DoDemolish(ref state, cmd, in occupancy);
                    break;
            }
        }

        private void DoBuild(
            ref SystemState state,
            in InputCommand cmd,
            in GridOccupancySingleton occupancy,
            in GridConfigSingleton gridConfig,
            Entity prefab)
        {
            if (occupancy.Map.ContainsKey(cmd.Cell)) return;

            var entity = state.EntityManager.Instantiate(prefab);
            state.EntityManager.SetComponentData(entity, new GridPosition { Cell = cmd.Cell });
            state.EntityManager.SetComponentData(entity, LocalTransform.FromPosition(
                GridUtility.CellToWorld(cmd.Cell, gridConfig)));

            occupancy.Map.Add(cmd.Cell, entity);
        }

        private void DoDemolish(
            ref SystemState state,
            in InputCommand cmd,
            in GridOccupancySingleton occupancy)
        {
            if (!occupancy.Map.TryGetValue(cmd.Cell, out var entity)) return;

            state.EntityManager.DestroyEntity(entity);
            occupancy.Map.Remove(cmd.Cell);
        }
    }
}