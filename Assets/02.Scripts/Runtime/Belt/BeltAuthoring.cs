using MokoIndustry.Foundation.Common;
using MokoIndustry.Foundation.Build;
using Unity.Entities;
using UnityEngine;

namespace MokoIndustry.Belt
{
    public class BeltAuthoring : MonoBehaviour
    {
        private class Baker : Baker<BeltAuthoring>
        {
            public override void Bake(BeltAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<BeltTag>(entity);
                AddComponent(entity, new GridPosition { Cell = default });
                AddComponent(entity, new BeltSegment
                {
                    Direction = Direction4.North,
                    Items = default,
                    XOffsets = default,
                    YPositions = default,
                    Length = 0
                });

                AddComponent<NewlyBuiltTag>(entity);
                SetComponentEnabled<NewlyBuiltTag>(entity, true);

                AddComponent<ItemRendererNeedsSpawnTag>(entity);
                SetComponentEnabled<ItemRendererNeedsSpawnTag>(entity, true);

                AddComponent<PendingDestroyTag>(entity);
                SetComponentEnabled<PendingDestroyTag>(entity, false);
            }
        }
    }
}