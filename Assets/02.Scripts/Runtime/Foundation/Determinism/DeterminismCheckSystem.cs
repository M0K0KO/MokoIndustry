using MokoIndustry.Foundation.Build;
using MokoIndustry.Foundation.Interpolation;
using MokoIndustry.Foundation.Tick;
using Unity.Collections;
using Unity.Entities;

namespace MokoIndustry.Foundation.Determinism
{
    [UpdateInGroup(typeof(FoundationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(TickTimingUpdateSystem))]
    public partial struct DeterminismCheckSystem : ISystem
    {
        private EntityQuery _gridPositionQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickSingleton>();

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new DeterminismHashSingleton
            {
                CurrentHash = 0UL,
                LastCheckedTick = -1,
                CheckInterval = 30
            });

            _gridPositionQuery = SystemAPI.QueryBuilder().WithAll<GridPosition>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var tick = SystemAPI.GetSingleton<TickSingleton>();
            ref var hashState = ref SystemAPI.GetSingletonRW<DeterminismHashSingleton>().ValueRW;

            if (tick.Current - hashState.LastCheckedTick < hashState.CheckInterval)
                return;

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
                hash = FnvCombine(hash, sortable[i]);
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

        private static ulong FnvCombine(ulong hash, ulong value)
        {
            const ulong prime = 1099511628211UL;
            hash ^= value;
            hash *= prime;
            return hash;
        }
    }
}