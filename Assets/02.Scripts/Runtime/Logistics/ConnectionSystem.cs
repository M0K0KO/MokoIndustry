using MokoIndustry.Belt;
using MokoIndustry.Foundation.Build;
using MokoIndustry.Foundation.Common;
using MokoIndustry.Foundation.Grid;
using MokoIndustry.Foundation.Input;
using MokoIndustry.Machine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MokoIndustry.Logistics
{
    [UpdateInGroup(typeof(CommandApplySystemGroup))]
    public partial struct ConnectionSystem : ISystem
    {
        static readonly int2[] s_Offsets =
{
            new int2(0, 1),   // North
            new int2(1, 0),   // East
            new int2(0, -1),  // South
            new int2(-1, 0),  // West
        };

        public void OnCreate(ref SystemState state) 
        {
            state.RequireForUpdate<GridOccupancySingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var occ = SystemAPI.GetSingleton<GridOccupancySingleton>();
            var reg = SystemAPI.GetSingleton<RecipeRegistrySingleton>();

            var dirty = new NativeParallelHashSet<int2>(64, Allocator.TempJob);
            var originEntities = new NativeList<Entity>(16, Allocator.TempJob);

            var collectBuilt = new CollectDirtyCellsJob
            {
                Dirty = dirty.AsParallelWriter(),
                Origins = originEntities.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            var collectDestroy = new CollectDestroyNeighborsJob
            {
                Dirty = dirty.AsParallelWriter()
            }.ScheduleParallel(collectBuilt);

            collectDestroy.Complete();

            var dirtyCells = dirty.ToNativeArray(Allocator.TempJob);
            dirty.Dispose();

            var rebuildHandle = new RebuildPortsJob
            {
                DirtyCells = dirtyCells,
                Occupancy = occ.Map,
                RecipeRegistry = reg,

                IOLookup = SystemAPI.GetComponentLookup<IOPort>(false),
                BeltLookup = SystemAPI.GetComponentLookup<BeltSegment>(true),
                RouterLookup = SystemAPI.GetComponentLookup<RouterTag>(true),
                MachineRecipeRefLookup = SystemAPI.GetComponentLookup<MachineRecipeRef>(true),
                DestroyLookup = SystemAPI.GetComponentLookup<PendingDestroyTag>(true),
            }.Schedule(dirtyCells.Length, 32, collectDestroy);

            rebuildHandle = dirtyCells.Dispose(rebuildHandle);

            var clearHandle = new ClearNewlyBuiltJob
            {
                Origins = originEntities,
                TagLookup = SystemAPI.GetComponentLookup<NewlyBuiltTag>(false)
            }.Schedule(rebuildHandle);

            state.Dependency = originEntities.Dispose(clearHandle);

            state.Dependency.Complete();
        }

        [BurstCompile]
        [WithAll(typeof(NewlyBuiltTag))]
        private partial struct CollectDirtyCellsJob : IJobEntity
        {
            public NativeParallelHashSet<int2>.ParallelWriter Dirty;
            public NativeList<Entity>.ParallelWriter Origins;

            private void Execute(in GridPosition pos, Entity entity)
            {
                var c = pos.Cell;

                Dirty.Add(c);
                Dirty.Add(c + new int2(0, 1));   // North
                Dirty.Add(c + new int2(1, 0));   // East
                Dirty.Add(c + new int2(0, -1));  // South
                Dirty.Add(c + new int2(-1, 0));  // West

                Origins.AddNoResize(entity);
            }
        }

        [BurstCompile]
        [WithAll(typeof(PendingDestroyTag))]
        private partial struct CollectDestroyNeighborsJob : IJobEntity
        {
            public NativeParallelHashSet<int2>.ParallelWriter Dirty;

            private void Execute(in GridPosition pos)
            {
                var c = pos.Cell;
                Dirty.Add(c + new int2(0, 1));   // North
                Dirty.Add(c + new int2(1, 0));   // East
                Dirty.Add(c + new int2(0, -1));  // South
                Dirty.Add(c + new int2(-1, 0));  // West
            }
        }

        [BurstCompile]
        private partial struct RebuildPortsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int2> DirtyCells;
            [ReadOnly] public NativeParallelHashMap<int2, Entity> Occupancy;
            [ReadOnly] public RecipeRegistrySingleton RecipeRegistry;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<IOPort> IOLookup;

            [ReadOnly] public ComponentLookup<BeltSegment> BeltLookup;
            [ReadOnly] public ComponentLookup<RouterTag> RouterLookup;
            [ReadOnly] public ComponentLookup<MachineRecipeRef> MachineRecipeRefLookup;
            [ReadOnly] public ComponentLookup<PendingDestroyTag> DestroyLookup;

            public void Execute(int index)
            {
                var cell = DirtyCells[index];

                if (!Occupancy.TryGetValue(cell, out var e))
                    return;

                if (!IOLookup.HasComponent(e))
                    return;

                if (IsDestroying(e))
                    return;

                IOPort port = default;

                if (BeltLookup.HasComponent(e))
                {
                    var dir = BeltLookup[e].Direction;

                    port.OutputMask = Direction4Extensions.Bit((int)dir);
                    port.InputMask = Direction4Extensions.Bit(
                        (int)Direction4Extensions.Opposite(dir));
                }
                else if (RouterLookup.HasComponent(e))
                {
                    for (int d = 0; d < 4; d++)
                    {
                        var dir = (Direction4)d;
                        var n = cell + Direction4Extensions.ToOffset(dir);

                        if (!Occupancy.TryGetValue(n, out var ne))
                            continue;

                        if (!IOLookup.HasComponent(ne))
                            continue;

                        if (IsDestroying(ne))
                            return;

                        var neighborDirToMe = Direction4Extensions.Opposite(dir);
                        if (BeltLookup.HasComponent(ne) &&
                            BeltLookup[ne].Direction == neighborDirToMe)
                        {
                            port.InputMask |= Direction4Extensions.Bit(d);
                        }
                        else if (MachineRecipeRefLookup.HasComponent(ne))
                        {
                            var neighborRecipeRef = MachineRecipeRefLookup[ne];
                            var neighborRecipe = RecipeRegistry.Get(neighborRecipeRef.Id);

                            if (neighborRecipe.Outputs.Length > 0)
                                port.InputMask |= Direction4Extensions.Bit(d);

                            if (neighborRecipe.Inputs.Length > 0)
                                port.OutputMask |= Direction4Extensions.Bit(d);
                        }
                        else
                        {
                            port.OutputMask |= Direction4Extensions.Bit(d);
                        }
                    }
                }
                else if (MachineRecipeRefLookup.HasComponent(e))
                {
                    var recipeRef = MachineRecipeRefLookup[e];
                    var recipe = RecipeRegistry.Get(recipeRef.Id);

                    byte inputMask = recipe.Inputs.Length > 0 ? (byte)0b1111 : (byte)0;
                    byte outputMask = recipe.Outputs.Length > 0 ? (byte)0b1111 : (byte)0;

                    byte acceptFilter = 0;
                    for (int i = 0; i < recipe.Inputs.Length; i++)
                        acceptFilter |= (byte)(1 << recipe.Inputs[i].ItemId);

                    port = new IOPort
                    {
                        InputMask = inputMask,
                        OutputMask = outputMask,
                        AcceptFilter = acceptFilter
                    };
                }

                IOLookup[e] = port;
            }

            private bool IsDestroying(Entity e)
                => DestroyLookup.HasComponent(e) && DestroyLookup.IsComponentEnabled(e);
        }

        [BurstCompile]
        private partial struct ClearNewlyBuiltJob : IJob
        {
            [ReadOnly] public NativeList<Entity> Origins;
            public ComponentLookup<NewlyBuiltTag> TagLookup;

            public void Execute()
            {
                for (int i = 0; i < Origins.Length; i++)
                    TagLookup.SetComponentEnabled(Origins[i], false);
            }
        }
    }
}