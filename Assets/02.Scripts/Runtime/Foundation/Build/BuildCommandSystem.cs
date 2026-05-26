using MokoIndustry.Foundation.Grid;
using MokoIndustry.Foundation.Input;
using MokoIndustry.Foundation.Tick;
using Unity.Entities;

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
        }

        public void OnUpdate(ref SystemState state)
        {
            var tick = SystemAPI.GetSingleton<TickSingleton>();
            var config = SystemAPI.GetSingleton<GridConfigSingleton>();
            var occupancy = SystemAPI.GetSingleton<GridOccupancySingleton>();

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

            int writeIdx = 0;
            for (int readIdx = 0; readIdx < buffer.Length; readIdx++)
            {
                var cmd = buffer[readIdx].Command;

                if (cmd.TargetTick > tick.Current)
                {
                    buffer[writeIdx++] = buffer[readIdx];
                    continue;
                }

                if (cmd.TargetTick < tick.Current)
                {
                    UnityEngine.Debug.LogWarning($"[Build] Stale command: TargetTick={cmd.TargetTick}, Current={tick.Current}");
                    continue;
                }

                Apply(ref state, cmd, in config, in occupancy);
            }

            int removed = buffer.Length - writeIdx;
            if (removed > 0)
                buffer.Length = writeIdx;
        }

        private void Apply(
            ref SystemState state,
            in InputCommand cmd,
            in GridConfigSingleton config,
            in GridOccupancySingleton occupancy)
        {
            if (!GridUtility.IsInBounds(cmd.Cell, in config)) return;

            switch (cmd.Type)
            {
                case CommandType.Build:
                    DoBuild(ref state, cmd, in occupancy);
                    break;
                case CommandType.Demolish:
                    DoDemolish(ref state, cmd, in occupancy);
                    break;
            }
        }

        private void DoBuild(
            ref SystemState state,
            in InputCommand cmd,
            in GridOccupancySingleton occupancy)
        {
            if (occupancy.Map.ContainsKey(cmd.Cell)) return;

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<DummyBuildingTag>(entity);
            state.EntityManager.AddComponentData(entity, new GridPosition { Cell = cmd.Cell });

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