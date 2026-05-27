using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace MokoIndustry.Belt
{
    public class ItemRendererAuthoring : MonoBehaviour
    {
        private class Baker : Baker<ItemRendererAuthoring>
        {
            public override void Bake(ItemRendererAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ItemRendererTag>(entity);
                AddComponent(entity, new ItemRendererState
                {
                    OwnerBelt = Entity.Null,
                    ArrayIndex = 0,
                });
                AddComponent(entity, new URPMaterialPropertyBaseColor
                {
                    Value = new float4(1f, 1f, 1f, 1f),
                });
            }
        }
    }
}