using MokoIndustry.Belt;
using MokoIndustry.Foundation.Build;
using MokoIndustry.Foundation.Common;
using MokoIndustry.Foundation.Grid;
using MokoIndustry.Foundation.Input;
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

                IOLookup = SystemAPI.GetComponentLookup<IOPort>(false),
                BeltLookup = SystemAPI.GetComponentLookup<BeltSegment>(true),
                RouterLookup = SystemAPI.GetComponentLookup<RouterTag>(true),
                DestroyLookup = SystemAPI.GetComponentLookup<PendingDestroyTag>(true),
            }.Schedule(dirtyCells.Length, 32, collectDestroy);

            rebuildHandle = dirtyCells.Dispose(rebuildHandle);

            var clearHandle = new ClearNewlyBuiltJob
            {
                Origins = originEntities,
                TagLookup = SystemAPI.GetComponentLookup<NewlyBuiltTag>(false)
            }.Schedule(rebuildHandle);

            state.Dependency = originEntities.Dispose(clearHandle);
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

            [NativeDisableParallelForRestriction]
            public ComponentLookup<IOPort> IOLookup;

            [ReadOnly] public ComponentLookup<BeltSegment> BeltLookup;
            [ReadOnly] public ComponentLookup<RouterTag> RouterLookup;
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
                        var n = cell + Offset(d);

                        if (!Occupancy.TryGetValue(n, out var ne))
                            continue;

                        if (!IOLookup.HasComponent(ne))
                            continue;

                        if (IsDestroying(ne))
                            return;

                        var neighborDirToMe = Direction4Extensions.Opposite((Direction4)d);

                        if (BeltLookup.HasComponent(ne) &&
                            BeltLookup[ne].Direction == neighborDirToMe)
                        {
                            port.InputMask |= Direction4Extensions.Bit(d);
                        }
                        else
                        {
                            port.OutputMask |= Direction4Extensions.Bit(d);
                        }
                    }
                }

                IOLookup[e] = port;
            }

            private bool IsDestroying(Entity e)
                => DestroyLookup.HasComponent(e) && DestroyLookup.IsComponentEnabled(e);

            private static int2 Offset(int d)
            {
                return d switch
                {
                    0 => new int2(0, 1),    // North
                    1 => new int2(1, 0),    // East
                    2 => new int2(0, -1),   // South
                    3 => new int2(-1, 0),   // West
                    _ => int2.zero
                };
            }
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