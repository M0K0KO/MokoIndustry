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

                if ((ItemId)src.Slots[0] == ItemId.None)
                    return;

                if (!WillReachNext(src.GetProgress(0)))
                    return;

                int2 nextCell = gridPos.Cell + src.Direction.ToOffset();

                if (!CellToIndex.TryGetValue(nextCell, out int destIdx)) 
                    return;

                if (destIdx == idx) 
                    return;

                var dest = Snapshots[destIdx];

                if (dest.GetSlot(BeltSegment.SlotCount - 1) != ItemId.None)
                    return;

                Intents.AddNoResize(new MoveIntent
                {
                    SourceIdx = idx,
                    DestIdx = destIdx,
                    Item = (ItemId)src.Slots[0],
                    Priority = (byte)src.Direction
                });
            }

            private static bool WillReachNext(byte progress)
            {
                return progress + BeltConstants.SpeedPerTickByte >= BeltConstants.MaxProgress;
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
            [ReadOnly] public NativeArray<byte> AcceptedOut;
            [ReadOnly] public NativeArray<ItemId> AcceptedIn;

            void Execute(
                ref BeltSegment belt,
                in GridPosition gridPos)
            {
                if (!CellToIndex.TryGetValue(gridPos.Cell, out int idx))
                    return;

                bool outgoing = AcceptedOut[idx] != 0;
                ItemId incoming = AcceptedIn[idx];

                StepBelt(ref belt, outgoing, incoming);
            }

            private static void StepBelt(
                ref BeltSegment belt,
                bool outgoing,
                ItemId incoming)
            {
                ItemId s0 = belt.GetSlot(0);
                ItemId s1 = belt.GetSlot(1);
                ItemId s2 = belt.GetSlot(2);
                ItemId s3 = belt.GetSlot(3);

                byte p0 = belt.GetProgress(0);
                byte p1 = belt.GetProgress(1);
                byte p2 = belt.GetProgress(2);
                byte p3 = belt.GetProgress(3);

                ItemId n0 = s0;
                ItemId n1 = s1;
                ItemId n2 = s2;
                ItemId n3 = s3;

                byte np0 = p0;
                byte np1 = p1;
                byte np2 = p2;
                byte np3 = p3;

                // 1. slot0 -> next belt
                if (s0 != ItemId.None)
                {
                    p0 = Advance(p0);

                    if (outgoing && p0 >= BeltConstants.MaxProgress)
                    {
                        n0 = ItemId.None;
                        np0 = 0;
                    }
                    else
                    {
                        n0 = s0;
                        np0 = p0;
                    }
                }

                // 2. slot1 -> slot0
                if (s1 != ItemId.None)
                {
                    p1 = Advance(p1);

                    if (p1 >= BeltConstants.MaxProgress && n0 == ItemId.None)
                    {
                        n0 = s1;
                        np0 = 0;

                        n1 = ItemId.None;
                        np1 = 0;
                    }
                    else
                    {
                        n1 = s1;
                        np1 = p1;
                    }
                }

                // 3. slot2 -> slot1
                if (s2 != ItemId.None)
                {
                    p2 = Advance(p2);

                    if (p2 >= BeltConstants.MaxProgress && n1 == ItemId.None)
                    {
                        n1 = s2;
                        np1 = 0;

                        n2 = ItemId.None;
                        np2 = 0;
                    }
                    else
                    {
                        n2 = s2;
                        np2 = p2;
                    }
                }

                // 4. slot3 -> slot2
                if (s3 != ItemId.None)
                {
                    p3 = Advance(p3);

                    if (p3 >= BeltConstants.MaxProgress && n2 == ItemId.None)
                    {
                        n2 = s3;
                        np2 = 0;

                        n3 = ItemId.None;
                        np3 = 0;
                    }
                    else
                    {
                        n3 = s3;
                        np3 = p3;
                    }
                }

                // 5. incoming -> slot3
                if (incoming != ItemId.None && n3 == ItemId.None)
                {
                    n3 = incoming;
                    np3 = 0;
                }

                belt.SetItem(0, n0, np0);
                belt.SetItem(1, n1, np1);
                belt.SetItem(2, n2, np2);
                belt.SetItem(3, n3, np3);
            }

            private static byte Advance(byte progress)
            {
                int next = progress + BeltConstants.SpeedPerTickByte;
                return next >= BeltConstants.MaxProgress
                    ? BeltConstants.MaxProgress
                    : (byte)next;
            }
        }
    }
}