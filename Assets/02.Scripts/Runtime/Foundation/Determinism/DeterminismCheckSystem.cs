using MokoIndustry.Belt;
using MokoIndustry.Foundation.Build;
using MokoIndustry.Foundation.Grid;
using MokoIndustry.Foundation.Interpolation;
using MokoIndustry.Foundation.Tick;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MokoIndustry.Foundation.Determinism
{
    [UpdateInGroup(typeof(FoundationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(TickTimingUpdateSystem))]
    [BurstCompile]
    public partial struct DeterminismCheckSystem : ISystem
    {
        private EntityQuery _gridPositionQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickSingleton>();
            state.RequireForUpdate<GridOccupancySingleton>();

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new DeterminismHashSingleton
            {
                CurrentHash = 0UL,
                LastCheckedTick = -1,
                CheckInterval = 30
            });

            _gridPositionQuery = SystemAPI.QueryBuilder().WithAll<GridPosition>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tick = SystemAPI.GetSingleton<TickSingleton>();
            ref var hashState = ref SystemAPI.GetSingletonRW<DeterminismHashSingleton>().ValueRW;

            if (tick.Current - hashState.LastCheckedTick < hashState.CheckInterval)
                return;

            var occupancy = SystemAPI.GetSingleton<GridOccupancySingleton>();
            var beltLookup = SystemAPI.GetComponentLookup<BeltSegment>(true);

            var positions = _gridPositionQuery.ToComponentDataArray<GridPosition>(Allocator.TempJob);
            var sortable = new NativeArray<ulong>(positions.Length, Allocator.TempJob);

            for (int i =0; i < positions.Length; i++)
            {
                sortable[i] = PackInt2((uint)positions[i].Cell.x, (uint)positions[i].Cell.y);
            }
            sortable.Sort();

            ulong hash = 14695981039346656037UL; // FNV-1a offset basis
            for (int i =0; i < sortable.Length; i++)
            {
                ulong packed = sortable[i];
                hash = FnvCombine(hash, packed);

                int2 cell = UnpackInt2(packed);

                if (occupancy.Map.TryGetValue(cell, out var entity) && entity != Entity.Null && beltLookup.HasComponent(entity))
                {
                    var belt = beltLookup[entity];
                    hash = FnvCombine(hash, (ulong)(byte)belt.Direction);
                    hash = FnvCombine(hash, PackSlots(belt.Slots));
                    hash = FnvCombine(hash, PackSlots(belt.SlotProgress));
                }
            }

            hashState.CurrentHash = hash;
            hashState.LastCheckedTick = tick.Current;

            positions.Dispose();
            sortable.Dispose();
        }

        private static ulong PackInt2(uint x, uint y)
        {
            return ((ulong)x << 32) | y;
        }

        private static int2 UnpackInt2(ulong packed)
        {
            uint x = (uint)(packed >> 32);
            uint y = (uint)(packed & 0xFFFFFFFFUL);
            return new int2((int)x, (int)y);
        }

        private static ulong PackSlots(in FixedList32Bytes<byte> list)
        {
            ulong packed = 0;
            packed |= (ulong)list[0];
            packed |= (ulong)list[1] << 8;
            packed |= (ulong)list[2] << 16;
            packed |= (ulong)list[3] << 24;
            return packed;
        }

        private static ulong FnvCombine(ulong hash, ulong value)
        {
            const ulong prime = 1099511628211UL;
            hash ^= value;
            hash *= prime;
            return hash;
        }
    }
}