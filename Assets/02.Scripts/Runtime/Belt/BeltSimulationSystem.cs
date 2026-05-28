using MokoIndustry.Foundation;
using MokoIndustry.Foundation.Build;
using MokoIndustry.Foundation.Common;
using MokoIndustry.Foundation.Grid;
using MokoIndustry.Foundation.Input;
using MokoIndustry.Logistics;
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
        private EntityQuery _routerQuery;

        public void OnCreate(ref SystemState state)
        {
            _beltQuery = SystemAPI.QueryBuilder()
                .WithAll<BeltTag, BeltSegment, GridPosition>()
                .Build();

            _routerQuery = SystemAPI.QueryBuilder()
                .WithAll<RouterTag, RouterSegment, GridPosition, IOPort>()
                .Build();

            state.RequireForUpdate<GridOccupancySingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int beltCount = _beltQuery.CalculateEntityCount();
            int routerCount = _routerQuery.CalculateEntityCount();
            int total = beltCount + routerCount;
            if (total == 0) return;

            var snapshots = new NativeArray<BeltSnapshot>(total, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var cellToIndex = new NativeParallelHashMap<int2, int>(total, Allocator.TempJob);
            var intents = new NativeList<MoveIntent>(total, Allocator.TempJob);
            var acceptedOut = new NativeArray<byte>(total, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            var acceptedIn = new NativeArray<ItemId>(total, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            var acceptedOutDir = new NativeArray<byte>(total, Allocator.TempJob, NativeArrayOptions.ClearMemory);



            var h1a = new BeltSnapshotJob
            {
                Snapshots = snapshots,
                CellToIndex = cellToIndex.AsParallelWriter(),
            }.ScheduleParallel(_beltQuery, state.Dependency);

            var h1b = new RouterSnapshotJob
            {
                Snapshots = snapshots,
                CellToIndex = cellToIndex.AsParallelWriter(),
                IndexOffset = beltCount,
            }.ScheduleParallel(_routerQuery, h1a);




            var h2a = new BeltIntentJob
            {
                Snapshots = snapshots,
                CellToIndex = cellToIndex,
                Intents = intents.AsParallelWriter(),
            }.ScheduleParallel(_beltQuery, h1b);

            var h2b = new RouterIntentJob
            {
                Snapshots = snapshots,
                CellToIndex = cellToIndex,
                Intents = intents.AsParallelWriter(),
                IndexOffset = beltCount,
            }.ScheduleParallel(_routerQuery, h2a);



            var h3 = new ResolveJob
            {
                Intents = intents,
                AcceptedOut = acceptedOut,
                AcceptedIn = acceptedIn,
                AcceptedOutDir = acceptedOutDir,
            }.Schedule(h2b);



            var h4a = new BeltApplyJob
            {
                CellToIndex = cellToIndex,
                AcceptedOut = acceptedOut,
                AcceptedIn = acceptedIn,
            }.ScheduleParallel(_beltQuery, h3);

            var h4b = new RouterApplyJob
            {
                CellToIndex = cellToIndex,
                AcceptedOut = acceptedOut,
                AcceptedIn = acceptedIn,
                AcceptedOutDir = acceptedOutDir,
            }.ScheduleParallel(_routerQuery, h4a);

            h4b.Complete();

            var d1 = snapshots.Dispose(h4b);
            var d2 = cellToIndex.Dispose(h4b);
            var d3 = intents.Dispose(h4b);
            var d4 = acceptedOut.Dispose(h4b);
            var d5 = acceptedIn.Dispose(h4b);
            var d6 = acceptedOutDir.Dispose(h4b);

            state.Dependency = JobHandle.CombineDependencies(
                JobHandle.CombineDependencies(d1, d2, d3),
                JobHandle.CombineDependencies(d4, d5, d6)
            );
        }

        [BurstCompile]
        private partial struct BeltSnapshotJob : IJobEntity
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
        private partial struct RouterSnapshotJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public NativeArray<BeltSnapshot> Snapshots;
            public NativeParallelHashMap<int2, int>.ParallelWriter CellToIndex;
            public int IndexOffset;

            void Execute(
                [EntityIndexInQuery] int localIdx,
                Entity entity,
                in RouterSegment router,
                in GridPosition gridPos)
            {
                int idx = IndexOffset + localIdx;

                byte len = (byte)router.Buffer.Length;

                Snapshots[idx] = new BeltSnapshot
                {
                    Entity = entity,
                    Kind = BeltSnapshot.KindRouter,

                    HeadItem = (len == 0) ? ItemId.None : (ItemId)router.Buffer[0],
                    ReadyToOutput = len > 0,
                    CanAcceptIn = len < RouterSegment.Capacity,
                };

                CellToIndex.TryAdd(gridPos.Cell, idx);
            }
        }

        [BurstCompile]
        private partial struct BeltIntentJob : IJobEntity
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
        private partial struct RouterIntentJob : IJobEntity
        {
            [ReadOnly] public NativeArray<BeltSnapshot> Snapshots;
            [ReadOnly] public NativeParallelHashMap<int2, int> CellToIndex;
            public NativeList<MoveIntent>.ParallelWriter Intents;
            public int IndexOffset;

            void Execute(
                [EntityIndexInQuery] int localIdx,
                in RouterSegment router,
                in IOPort port,
                in GridPosition gridPos)
            {
                int idx = IndexOffset + localIdx;
                var src = Snapshots[idx];

                if (!src.ReadyToOutput) return;

                for (int step = 0; step < 4; step++)
                {
                    int d = (router.RoundRobinPtr + step) & 3;
                    if (!Direction4Extensions.Has(port.OutputMask, d)) continue;

                    int2 nextCell = gridPos.Cell + Direction4Extensions.ToOffset((Direction4)d);
                    if (!CellToIndex.TryGetValue(nextCell, out int destIdx)) continue;
                    if (destIdx == idx) continue;

                    var dest = Snapshots[destIdx];
                    if (!dest.CanAcceptIn) continue;

                    Intents.AddNoResize(new MoveIntent
                    {
                        SourceIdx = idx,
                        DestIdx = destIdx,
                        Item = src.HeadItem,
                        Priority = (byte)d,
                        SourceChosenDir = (byte)d,
                    });
                    return;
                }
            }
        }

        [BurstCompile]
        private partial struct ResolveJob : IJob
        {
            public NativeList<MoveIntent> Intents;
            public NativeArray<byte> AcceptedOut;
            public NativeArray<byte> AcceptedOutDir;
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
                    AcceptedOutDir[winner.SourceIdx] = winner.SourceChosenDir;
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
                c = a.Priority.CompareTo(b.Priority);
                if (c != 0) return c;
                return a.SourceIdx.CompareTo(b.SourceIdx); // tie breaker
            }
        }

        [BurstCompile]
        private partial struct BeltApplyJob : IJobEntity
        {
            [ReadOnly] public NativeParallelHashMap<int2, int> CellToIndex;
            [ReadOnly] public NativeArray<byte>   AcceptedOut;
            [ReadOnly] public NativeArray<ItemId> AcceptedIn;

            void Execute(
                ref BeltSegment belt,
                in GridPosition gridPos)
            {
                belt.PrevYPositions = belt.YPositions;
                belt.PrevXOffsets = belt.XOffsets;
                belt.PrevLength = belt.Length;

                if (!CellToIndex.TryGetValue(gridPos.Cell, out int idx))
                    return;

                bool outgoing = AcceptedOut[idx] != 0;
                ItemId incoming = AcceptedIn[idx];

                if (outgoing) belt.RemoveHead();

                for (int i = belt.Length-1; i >= 0; i--)
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

        [BurstCompile]
        private partial struct RouterApplyJob : IJobEntity
        {
            [ReadOnly] public NativeParallelHashMap<int2, int> CellToIndex;
            [ReadOnly] public NativeArray<byte> AcceptedOut;
            [ReadOnly] public NativeArray<ItemId> AcceptedIn;
            [ReadOnly] public NativeArray<byte> AcceptedOutDir;

            void Execute(
               ref RouterSegment router,
               in GridPosition gridPos)
            {
                if (!CellToIndex.TryGetValue(gridPos.Cell, out int idx))
                    return;

                bool outgoing = AcceptedOut[idx] != 0;
                ItemId incoming = AcceptedIn[idx];

                if (outgoing && router.Buffer.Length > 0)
                {
                    router.Buffer.RemoveAt(0);
                    int chosenDir = AcceptedOutDir[idx];
                    router.RoundRobinPtr = (byte)((chosenDir + 1) & 3);
                }

                if (incoming != ItemId.None && router.Buffer.Length < RouterSegment.Capacity)
                {
                    router.Buffer.Add((byte)incoming);
                }
            }
        }
    }
}