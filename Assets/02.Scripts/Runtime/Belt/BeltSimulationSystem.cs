using MokoIndustry.Foundation;
using MokoIndustry.Foundation.Build;
using MokoIndustry.Foundation.Common;
using MokoIndustry.Foundation.Grid;
using MokoIndustry.Foundation.Input;
using MokoIndustry.Logistics;
using MokoIndustry.Machine;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MokoIndustry.Belt
{
    [UpdateInGroup(typeof(FoundationSystemGroup))]
    [UpdateAfter(typeof(CommandApplySystemGroup))]
    [UpdateAfter(typeof(MachineTickSystem))]
    [BurstCompile]
    public partial struct BeltSimulationSystem : ISystem
    {
        private EntityQuery _beltQuery;
        private EntityQuery _routerQuery;
        private EntityQuery _machineQuery;

        public void OnCreate(ref SystemState state)
        {
            _beltQuery = SystemAPI.QueryBuilder()
                .WithAll<BeltTag, BeltSegment, GridPosition>()
                .Build();

            _routerQuery = SystemAPI.QueryBuilder()
                .WithAll<RouterTag, RouterSegment, GridPosition, IOPort>()
                .Build();

            _machineQuery = SystemAPI.QueryBuilder()
                .WithAll<MachineTag, MachineState, GridPosition, IOPort>()
                .Build();

            state.RequireForUpdate<GridOccupancySingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int beltCount = _beltQuery.CalculateEntityCount();
            int routerCount = _routerQuery.CalculateEntityCount();
            int machineCount = _machineQuery.CalculateEntityCount();
            int total = beltCount + routerCount + machineCount;
            if (total == 0) return;

            var snapshots = new NativeArray<BeltSnapshot>(total, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var cellToIndex = new NativeParallelHashMap<int2, int>(total, Allocator.TempJob);
            var intents = new NativeList<MoveIntent>(total, Allocator.TempJob);
            var acceptedOut = new NativeArray<byte>(total, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            var acceptedIn = new NativeArray<ItemId>(total, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            var acceptedOutDir = new NativeArray<byte>(total, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            var acceptedInY = new NativeArray<byte>(total, Allocator.TempJob, NativeArrayOptions.ClearMemory);


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

            var h1c = new MachineSnapshotJob
            {
                Snapshots = snapshots,
                CellToIndex = cellToIndex.AsParallelWriter(),
                IndexOffset = beltCount + routerCount,
            }.ScheduleParallel(_machineQuery, h1b);




            var h2a = new BeltIntentJob
            {
                Snapshots = snapshots,
                CellToIndex = cellToIndex,
                Intents = intents.AsParallelWriter(),
            }.ScheduleParallel(_beltQuery, h1c);

            var h2b = new RouterIntentJob
            {
                Snapshots = snapshots,
                CellToIndex = cellToIndex,
                Intents = intents.AsParallelWriter(),
                IndexOffset = beltCount,
            }.ScheduleParallel(_routerQuery, h2a);

            var h2c = new MachineIntentJob
            {
                Snapshots = snapshots,
                CellToIndex = cellToIndex,
                Intents = intents.AsParallelWriter(),
                IndexOffset = beltCount + routerCount,
            }.ScheduleParallel(_machineQuery, h2b);



            var h3 = new ResolveJob
            {
                Intents = intents,
                AcceptedOut = acceptedOut,
                AcceptedIn = acceptedIn,
                AcceptedOutDir = acceptedOutDir,
                AcceptedInY = acceptedInY,
            }.Schedule(h2c);



            var h4a = new BeltApplyJob
            {
                Snapshots = snapshots,
                CellToIndex = cellToIndex,
                AcceptedOut = acceptedOut,
                AcceptedIn = acceptedIn,
                AcceptedInY = acceptedInY,
            }.ScheduleParallel(_beltQuery, h3);

            var h4b = new RouterApplyJob
            {
                CellToIndex = cellToIndex,
                AcceptedOut = acceptedOut,
                AcceptedIn = acceptedIn,
                AcceptedOutDir = acceptedOutDir,
            }.ScheduleParallel(_routerQuery, h4a);

            var h4c = new MachineApplyJob
            {
                CellToIndex = cellToIndex,
                AcceptedOut = acceptedOut,
                AcceptedIn = acceptedIn,
            }.ScheduleParallel(_machineQuery, h4b);

            h4c.Complete();

            var d1 = snapshots.Dispose(h4c);
            var d2 = cellToIndex.Dispose(h4c);
            var d3 = intents.Dispose(h4c);
            var d4 = acceptedOut.Dispose(h4c);
            var d5 = acceptedIn.Dispose(h4c);
            var d6 = acceptedInY.Dispose(h4c);
            var d7 = acceptedOutDir.Dispose(h4c);

            state.Dependency = JobHandle.CombineDependencies(
                JobHandle.CombineDependencies(d1, d2, d3),
                JobHandle.CombineDependencies(d4, d5, d6),
                d7
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
                in IOPort port,
                in GridPosition gridPos)
            {
                int idx = IndexOffset + localIdx;

                byte len = (byte)router.Buffer.Length;

                Snapshots[idx] = new BeltSnapshot
                {
                    Entity = entity,
                    Kind = BeltSnapshot.KindRouter,

                    HeadItem = (len == 0) ? ItemId.None : (ItemId)router.Buffer[0],
                    ReadyToOutput = len > 0 && router.OutputCooldown == 0 && port.OutputMask != 0,
                    CanAcceptIn = len < RouterSegment.Capacity && port.InputMask != 0,
                    InputMask = port.InputMask,
                    OutputMask = port.OutputMask,
                };

                CellToIndex.TryAdd(gridPos.Cell, idx);
            }
        }

        [BurstCompile]
        private partial struct MachineSnapshotJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public NativeArray<BeltSnapshot> Snapshots;
            public NativeParallelHashMap<int2, int>.ParallelWriter CellToIndex;
            public int IndexOffset;

            void Execute(
                [EntityIndexInQuery] int localIdx,
                Entity entity,
                in MachineState machine,
                in IOPort port,
                in GridPosition gridPos)
            {
                int idx = IndexOffset + localIdx;

                Snapshots[idx] = new BeltSnapshot
                {
                    Entity = entity,
                    Kind = BeltSnapshot.KindMachine,
                    HeadItem = machine.OutputBuffer.Length == 0 ? ItemId.None : (ItemId)machine.OutputBuffer[0],
                    ReadyToOutput = port.OutputMask != 0 && machine.OutputBuffer.Length > 0,
                    CanAcceptIn = port.InputMask != 0 && machine.InputBuffer.Length < machine.InputBuffer.Capacity,
                    InputMask = port.InputMask,
                    OutputMask = port.OutputMask,
                    AcceptFilter = port.AcceptFilter,
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

                if (!src.ReadyToOutput) return;

                int2 nextCell = gridPos.Cell + src.Direction.ToOffset();
                if (!CellToIndex.TryGetValue(nextCell, out int destIdx)) return;
                if (destIdx == idx) return;

                var dest = Snapshots[destIdx];

                if (!CanMoveInto(in dest, src.HeadItem, src.Direction)) return;

                int overflow = src.HeadY + BeltConstants.SpeedPerTick - BeltConstants.MaxPosition;
                byte carry = (byte)math.max(0, overflow);

                Intents.AddNoResize(new MoveIntent
                {
                    SourceIdx = idx,
                    DestIdx = destIdx,
                    Item = src.HeadItem,
                    Priority = (byte)src.Direction,
                    CarryY = carry
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
                    if (!CanMoveInto(in dest, src.HeadItem, (Direction4)d)) continue;

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
        private partial struct MachineIntentJob : IJobEntity
        {
            [ReadOnly] public NativeArray<BeltSnapshot> Snapshots;
            [ReadOnly] public NativeParallelHashMap<int2, int> CellToIndex;
            public NativeList<MoveIntent>.ParallelWriter Intents;
            public int IndexOffset;

            void Execute(
                [EntityIndexInQuery] int localIdx,
                in GridPosition gridPos)
            {
                int idx = IndexOffset + localIdx;
                var src = Snapshots[idx];
                if (!src.ReadyToOutput) return;

                for (int d = 0; d < 4; d++)
                {
                    if (!Direction4Extensions.Has(src.OutputMask, d)) continue;

                    var outputDir = (Direction4)d;
                    int2 nextCell = gridPos.Cell + Direction4Extensions.ToOffset(outputDir);
                    if (!CellToIndex.TryGetValue(nextCell, out int destIdx)) continue;
                    if (destIdx == idx) continue;

                    var dest = Snapshots[destIdx];
                    if (!CanMoveInto(in dest, src.HeadItem, outputDir)) continue;

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

        private static bool CanMoveInto(in BeltSnapshot dest, ItemId item, Direction4 sourceOutputDir)
        {
            if (!dest.CanAcceptIn) return false;

            if (dest.AcceptFilter != 0 && (dest.AcceptFilter & (1 << (byte)item)) == 0)
                return false;

            if (dest.InputMask == 0)
                return true;

            var incomingDir = Direction4Extensions.Opposite(sourceOutputDir);

            return Direction4Extensions.Has(dest.InputMask, (int)incomingDir);
        }

        [BurstCompile]
        private partial struct ResolveJob : IJob
        {
            public NativeList<MoveIntent> Intents;
            public NativeArray<byte> AcceptedOut;
            public NativeArray<byte> AcceptedOutDir;
            public NativeArray<ItemId> AcceptedIn;
            public NativeArray<byte> AcceptedInY;

            public void Execute()
            {
                if (Intents.Length == 0) return;

                Intents.Sort(new IntentComparer());

                int i = 0;
                while (i < Intents.Length)
                {
                    var winner = Intents[i];
                    AcceptedOut[winner.SourceIdx] = 1;
                    AcceptedOutDir[winner.SourceIdx] = winner.SourceChosenDir;
                    AcceptedIn[winner.DestIdx] = winner.Item;
                    AcceptedInY[winner.DestIdx] = winner.CarryY;
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
            [ReadOnly] public NativeArray<BeltSnapshot> Snapshots;
            [ReadOnly] public NativeParallelHashMap<int2, int> CellToIndex;
            [ReadOnly] public NativeArray<byte> AcceptedOut;
            [ReadOnly] public NativeArray<ItemId> AcceptedIn;
            [ReadOnly] public NativeArray<byte> AcceptedInY;

            void Execute(
                ref BeltSegment belt,
                in GridPosition gridPos)
            {
                belt.PrevItems = belt.Items;
                belt.PrevYPositions = belt.YPositions;
                belt.PrevXOffsets = belt.XOffsets;
                belt.PrevLength = belt.Length;
                belt.InsertedAtTailThisTick = 0;

                if (!CellToIndex.TryGetValue(gridPos.Cell, out int idx))
                    return;

                bool outgoing = AcceptedOut[idx] != 0;
                ItemId incoming = AcceptedIn[idx];

                if (outgoing) belt.RemoveHead();

                int headLimit = GetAlignedNextHeadLimit(idx, in belt, in gridPos);

                for (int i = belt.Length - 1; i >= 0; i--)
                {
                    int frontY = (i == belt.Length - 1)
                        ? headLimit + 1
                        : belt.YPositions[i + 1] - BeltConstants.ItemSpace;

                    int currentY = belt.YPositions[i];
                    int newY = math.min(currentY + BeltConstants.SpeedPerTick, frontY);
                    newY = math.min(newY, headLimit);
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
                    belt.InsertAtTail(incoming, 0, AcceptedInY[idx]);
                    belt.InsertedAtTailThisTick = 1;
                }
            }

            private int GetAlignedNextHeadLimit(int idx, in BeltSegment belt, in GridPosition gridPos)
            {
                int2 nextCell = gridPos.Cell + belt.Direction.ToOffset();
                if (!CellToIndex.TryGetValue(nextCell, out int nextIdx) || nextIdx == idx)
                {
                    return BeltConstants.MaxPosition;
                }

                var next = Snapshots[nextIdx];
                if (next.Kind != BeltSnapshot.KindBelt || next.Direction != belt.Direction)
                {
                    return BeltConstants.MaxPosition;
                }

                return BeltConstants.MaxPosition - math.max((int)BeltConstants.ItemSpace - next.MinPosition, 0);
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
                if (router.OutputCooldown > 0)
                    router.OutputCooldown--;

                if (!CellToIndex.TryGetValue(gridPos.Cell, out int idx))
                {
                    return;
                }

                bool outgoing = AcceptedOut[idx] != 0;
                ItemId incoming = AcceptedIn[idx];

                if (outgoing && router.Buffer.Length > 0)
                {
                    router.Buffer.RemoveAt(0);
                    int chosenDir = AcceptedOutDir[idx];
                    router.RoundRobinPtr = (byte)((chosenDir + 1) & 3);
                    router.OutputCooldown = BeltConstants.RouterOutputInterval;
                }

                if (incoming != ItemId.None && router.Buffer.Length < RouterSegment.Capacity)
                {
                    router.Buffer.Add((byte)incoming);
                }
            }
        }

        [BurstCompile]
        private partial struct MachineApplyJob : IJobEntity
        {
            [ReadOnly] public NativeParallelHashMap<int2, int> CellToIndex;
            [ReadOnly] public NativeArray<byte> AcceptedOut;
            [ReadOnly] public NativeArray<ItemId> AcceptedIn;

            void Execute(
                ref MachineState machine,
                in GridPosition gridPos)
            {
                if (!CellToIndex.TryGetValue(gridPos.Cell, out int idx))
                    return;

                if (AcceptedOut[idx] != 0 && machine.OutputBuffer.Length > 0)
                {
                    machine.OutputBuffer.RemoveAt(0);
                }

                ItemId incoming = AcceptedIn[idx];
                if (incoming != ItemId.None && machine.InputBuffer.Length < machine.InputBuffer.Capacity)
                {
                    machine.InputBuffer.Add((byte)incoming);
                }
            }
        }
    }
}