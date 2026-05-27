using MokoIndustry.Foundation;
using MokoIndustry.Foundation.Build;
using MokoIndustry.Foundation.Common;
using MokoIndustry.Foundation.Input;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MokoIndustry.Belt
{
    [UpdateInGroup(typeof(FoundationSystemGroup))]
    [UpdateAfter(typeof(CommandApplySystemGroup))]
    [BurstCompile]
    public partial struct BeltSimulationSystem : ISystem
    {
        private EntityQuery _beltQuery;

        public void OnCreate(ref SystemState state)
        {
            _beltQuery = SystemAPI.QueryBuilder()
                .WithAll<BeltTag, BeltSegment, GridPosition>()
                .Build();

            state.RequireForUpdate(_beltQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int count = _beltQuery.CalculateEntityCount();
            if (count == 0) return;

            var snapshots = new NativeArray<BeltSnapshot>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var cellToIndex = new NativeParallelHashMap<int2, int>(count, Allocator.TempJob);
            var intents = new NativeList<MoveIntent>(count, Allocator.TempJob);
            var acceptedOut = new NativeArray<bool>(count, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            var acceptedIn = new NativeArray<ItemId>(count, Allocator.TempJob, NativeArrayOptions.ClearMemory);

            var h1 = new SnapshotJob
            {
                Snapshots = snapshots,
                CellToIndex = cellToIndex.AsParallelWriter(),
            }.ScheduleParallel(_beltQuery, state.Dependency);

            var h2 = new IntentJob
            {
                Snapshots = snapshots,
                CellToIndex = cellToIndex,
                Intents = intents.AsParallelWriter(),
            }.ScheduleParallel(_beltQuery, h1);

            var h3 = new ResolveJob
            {
                Intents = intents,
                AcceptedOut = acceptedOut,
                AcceptedIn = acceptedIn,
            }.Schedule(h2);

            var h4 = new ApplyJob
            {
                CellToIndex = cellToIndex,
                AcceptedOut = acceptedOut,
                AcceptedIn = acceptedIn,
            }.ScheduleParallel(_beltQuery, h3);

            state.Dependency = h4;

            snapshots.Dispose(h4);
            cellToIndex.Dispose(h4);
            intents.Dispose(h4);
            acceptedOut.Dispose(h4);
            acceptedIn.Dispose(h4);
        }

        [BurstCompile]
        private partial struct SnapshotJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public NativeArray<BeltSnapshot> Snapshots;
            public NativeParallelHashMap<int2, int>.ParallelWriter CellToIndex;

            void Execute(
                [EntityIndexInQuery] int idx,
                Entity entity,
                in BeltSegment belt,
                in GridPosition gridPos)
            {
                Snapshots[idx] = new BeltSnapshot
                {
                    Entity = entity,
                    Direction = belt.Direction,
                    Slots = belt.Slots,
                    Progress = belt.Progress,
                };
                CellToIndex.TryAdd(gridPos.Cell, idx);
            }
        }

        [BurstCompile]
        private partial struct IntentJob : IJobEntity
        {
            [ReadOnly] public NativeArray<BeltSnapshot> Snapshots;
            [ReadOnly] public NativeParallelHashMap<int2, int> CellToIndex;
            public NativeList<MoveIntent>.ParallelWriter Intents;

            void Execute(
                [EntityIndexInQuery] int idx,
                in GridPosition gridPos)
            {
                var src = Snapshots[idx];

                if (src.Progress + BeltConstants.SpeedPerTick < 1f) return;

                var head = (ItemId)src.Slots[0];
                if (head == ItemId.None) return;

                int2 nextCell = gridPos.Cell + src.Direction.ToOffset();
                if (!CellToIndex.TryGetValue(nextCell, out int destIdx)) return;
                if (destIdx == idx) return;

                var dest = Snapshots[destIdx];
                if ((ItemId)dest.Slots[BeltConstants.SlotCount - 1] != ItemId.None) return;

                Intents.AddNoResize(new MoveIntent
                {
                    SourceIdx = idx,
                    DestIdx = destIdx,
                    Item = head,
                    Priority = (byte)src.Direction
                });
            }
        }

        [BurstCompile]
        private struct ResolveJob : IJob
        {
            public NativeList<MoveIntent> Intents;
            public NativeArray<bool> AcceptedOut;
            public NativeArray<ItemId> AcceptedIn;

            public void Execute()
            {
                if (Intents.Length == 0) return;

                Intents.Sort(new IntentComparer());

                int i = 0;
                while(i < Intents.Length )
                {
                    var winner = Intents[i];
                    AcceptedOut[winner.SourceIdx] = true;
                    AcceptedIn[winner.DestIdx] = winner.Item;

                    int destIdx = winner.DestIdx;
                    i++;
                    while (i < Intents.Length && Intents[i].DestIdx == destIdx)
                        i++;
                }
            }
        }

        private struct IntentComparer : IComparer<MoveIntent>
        {
            public int Compare(MoveIntent a, MoveIntent b)
            {
                int c = a.DestIdx.CompareTo(b.DestIdx);
                if (c != 0) return c;
                return a.Priority.CompareTo(b.Priority);
            }
        }

        [BurstCompile]
        private partial struct ApplyJob : IJobEntity
        {
            [ReadOnly] public NativeParallelHashMap<int2, int> CellToIndex;
            [ReadOnly] public NativeArray<bool> AcceptedOut;
            [ReadOnly] public NativeArray<ItemId> AcceptedIn;

            void Execute(
                ref BeltSegment belt,
                in GridPosition gridPos)
            {
                if (!CellToIndex.TryGetValue(gridPos.Cell, out int idx)) return;

                belt.Progress += BeltConstants.SpeedPerTick;

                bool outgoing = AcceptedOut[idx];
                ItemId incoming = AcceptedIn[idx];

                if (outgoing)
                {
                    belt.SetSlot(0, belt.GetSlot(1));
                    belt.SetSlot(1, belt.GetSlot(2));
                    belt.SetSlot(2, belt.GetSlot(3));
                    belt.SetSlot(3, ItemId.None);
                    belt.Progress -= 1f;
                }
                else if (belt.Progress >= 1f)
                {
                    if (belt.GetSlot(0) == ItemId.None)
                    {
                        belt.SetSlot(0, belt.GetSlot(1));
                        belt.SetSlot(1, belt.GetSlot(2));
                        belt.SetSlot(2, belt.GetSlot(3));
                        belt.SetSlot(3, ItemId.None);
                        belt.Progress -= 1f;
                    }
                    else
                    {
                        belt.Progress = 1f;
                    }
                }

                if (incoming != ItemId.None)
                {
                    belt.SetSlot(3, incoming);
                }
            }
        }
    }
}