using MokoIndustry.Foundation;
using MokoIndustry.Foundation.Tick;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace MokoIndustry.Machine
{
    [UpdateInGroup(typeof(FoundationSystemGroup))]
    [UpdateAfter(typeof(TickAdvanceSystem))]
    [BurstCompile]
    public partial struct MachineTickSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickSingleton>();
            state.RequireForUpdate<RecipeRegistrySingleton>();
            state.RequireForUpdate<MachineTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var registry = SystemAPI.GetSingleton<RecipeRegistrySingleton>();
            new MachineTickJob { Registry = registry }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct MachineTickJob : IJobEntity
    {
        [ReadOnly] public RecipeRegistrySingleton Registry;

        public void Execute(ref MachineState ms, in MachineRecipeRef recipeRef)
        {
            var recipe = Registry.Get(recipeRef.Id);

            // try to finish
            if (ms.ProgressTicks >= recipe.ProcessingTicks)
            {
                if (TryProduce(ref ms.OutputBuffer, in recipe.Outputs))
                {
                    ms.ProgressTicks = 0;
                }
                else
                {
                    ms.ProgressTicks = recipe.ProcessingTicks;
                    return;
                }
            }

            // try to start (same tick)
            if (ms.ProgressTicks == 0)
            {
                if (TryConsume(ref ms.InputBuffer, in recipe.Inputs))
                {
                    ms.ProgressTicks = 1;
                }
            }
            else
            {
                ms.ProgressTicks++;
            }
        }

        private bool TryProduce(ref FixedList64Bytes<byte> buf, in FixedList32Bytes<RecipeItem> outs)
        {
            // is there some space?
            int total = 0;
            for (int i = 0; i < outs.Length; i++) total += outs[i].Count;
            if (buf.Length + total > buf.Capacity) return false;

            for (int i = 0; i < outs.Length; i++)
                for (int c = 0; c < outs[i].Count; c++)
                    buf.Add(outs[i].ItemId);
            return true;
        }

        private bool TryConsume(ref FixedList64Bytes<byte> buf, in FixedList32Bytes<RecipeItem> req)
        {
            // requirement check
            for (int i = 0; i < req.Length; i++)
            {
                int count = 0;
                for (int j = 0; j < buf.Length; j++)
                    if (buf[j] == req[i].ItemId) count++;
                if (count < req[i].Count) return false;
            }

            // consume
            for (int i = 0; i < req.Length; i++)
            {
                byte remain = req[i].Count;
                for (int j = buf.Length - 1; j >= 0 && remain > 0; j--)
                {
                    if (buf[j] == req[i].ItemId) { buf.RemoveAt(j); remain--; }
                }
            }
            return true;
        }
    }
}