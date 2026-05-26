using Unity.Entities;
using UnityEngine;

namespace MokoIndustry.Foundation.Tick
{
    public class TickConfigAuthoring : MonoBehaviour
    {
        public uint Seed = 12345;

        class Baker : Baker<TickConfigAuthoring>
        {
            public override void Bake(TickConfigAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new TickSingleton
                {
                    Current = 0,
                    InputDelay = 0,
                    Seed = authoring.Seed,
                });
            }
        }
    }
}