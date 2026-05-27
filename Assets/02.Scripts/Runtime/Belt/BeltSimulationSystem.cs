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
            var acceptedOut = new NativeArray<byte>(count, Allocator.TempJob, NativeArrayOptions.ClearMemory);
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

            //state.Dependency = h4;
            h4.Complete();

            var d1 = snapshots.Dispose(h4);
            var d2 = cellToIndex.Dispose(h4);
            var d3 = intents.Dispose(h4);
            var d4 = acceptedOut.Dispose(h4);
            var d5 = acceptedIn.Dispose(h4);

            state.Dependency = JobHandle.CombineDependencies(
                JobHandle.CombineDependencies(d1, d2, d3),
                JobHandle.CombineDependencies(d4, d5)
            );
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
                Snapshots[idx] = BeltSnapshot.From(entity, belt);
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

                if (src.Length == 0) return;
                byte headY = src.YPositions[src.Length - 1];
                if (headY + BeltConstants.SpeedPerTick < BeltConstants.MaxPosition) return;

                int2 nextCell = gridPos.Cell + src.Direction.ToOffset();
                if (!CellToIndex.TryGetValue(nextCell, out int destIdx)) return;
                if (destIdx == idx) return;

                var dest = Snapshots[destIdx];
                if (dest.Length >= BeltConstants.Capacity) return;
                if (dest.MinPosition < BeltConstants.ItemSpace) return; // no space to accept

                Intents.AddNoResize(new MoveIntent
                {
                    SourceIdx = idx,
                    DestIdx = destIdx,
                    Item = (ItemId)src.Items[src.Length - 1],
                    Priority = (byte)src.Direction,
                });
            }
        }

        [BurstCompile]
        private struct ResolveJob : IJob
        {
            public NativeList<MoveIntent> Intents;
            public NativeArray<byte> AcceptedOut;
            public NativeArray<ItemId> AcceptedIn;

            public void Execute()
            {
                if (Intents.Length == 0) return;

                Intents.Sort(new IntentComparer());

                int i = 0;
                while(i < Intents.Length )
                {
                    var winner = Intents[i];
                    AcceptedOut[winner.SourceIdx] = 1;
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
            [ReadOnly] public NativeArray<byte>   AcceptedOut;
            [ReadOnly] public NativeArray<ItemId> AcceptedIn;

            void Execute(
                ref BeltSegment belt,
                in GridPosition gridPos)
            {
                if (!CellToIndex.TryGetValue(gridPos.Cell, out int idx))
                    return;

                bool outgoing = AcceptedOut[idx] != 0;
                ItemId incoming = AcceptedIn[idx];

                if (outgoing) belt.RemoveHead();

                for (int i =belt.Length-1; i>=0; i--)
                {
                    int frontY = (i == belt.Length - 1)
                        ? BeltConstants.MaxPosition + 1
                        : belt.YPositions[i + 1] - BeltConstants.ItemSpace;

                    int currentY = belt.YPositions[i];
                    int newY = math.min(currentY + BeltConstants.SpeedPerTick, frontY);
                    newY = math.min(newY, BeltConstants.MaxPosition);
                    if (newY < currentY) newY = currentY;

                    belt.YPositions[i] = (byte)newY;

                    int currentX = belt.XOffsets[i];
                    int xMove = BeltConstants.SpeedPerTick * 2;
                    if (currentX > 0) currentX = math.max(0, currentX - xMove);
                    else if (currentX < 0) currentX = math.min(0, currentX + xMove);
                    belt.XOffsets[i] = (sbyte)currentX;
                }

                if (incoming != ItemId.None 
                    && belt.Length < BeltConstants.Capacity 
                    && (belt.Length == 0 || belt.YPositions[0] >= BeltConstants.ItemSpace))
                {
                    belt.InsertAtTail(incoming, 0);
                }
            }
        }
    }
}