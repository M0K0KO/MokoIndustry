using MokoIndustry.Foundation.Build;
using Unity.Entities;
using UnityEngine;

namespace MokoIndustry.Foundation
{
    public class DummyBuildingAuthoring : MonoBehaviour
    {
        private class Baker : Baker<DummyBuildingAuthoring>
        {
            public override void Bake(DummyBuildingAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<BuildableTag>(entity);
                AddComponent(entity, new GridPosition { Cell = default });
            }
        }
    }
}