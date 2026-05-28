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

            var originEntities = new NativeList<Entity>(16, Allocator.TempJob);
            var dirty = new NativeParallelHashSet<int2>(64, Allocator.Temp);

            var collectHandle = new CollectDirtyCellsJob
            {
                Dirty = dirty.AsParallelWriter(),
                Origins = originEntities.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
            collectHandle.Complete();

            var dirtyCells = dirty.ToNativeArray(Allocator.TempJob);
            dirty.Dispose();



            var rebuildHandle = new RebuildPortsJob
            {
                DirtyCells = dirtyCells,
                Occupancy = occ.Map,

                IOLookup = SystemAPI.GetComponentLookup<IOPort>(false),
                BeltLookup = SystemAPI.GetComponentLookup<BeltSegment>(true),
                RouterLookup = SystemAPI.GetComponentLookup<RouterTag>(true),
            }.Schedule(dirtyCells.Length, 32, collectHandle);
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
        private partial struct RebuildPortsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int2> DirtyCells;
            [ReadOnly] public NativeParallelHashMap<int2, Entity> Occupancy;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<IOPort> IOLookup;

            [ReadOnly] public ComponentLookup<BeltSegment> BeltLookup;
            [ReadOnly] public ComponentLookup<RouterTag> RouterLookup;

            public void Execute(int index)
            {
                var cell = DirtyCells[index];

                if (!Occupancy.TryGetValue(cell, out var e))
                    return;

                if (!IOLookup.HasComponent(e))
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

                        var neighborDirToMe =
                            Direction4Extensions.Opposite((Direction4)d);

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