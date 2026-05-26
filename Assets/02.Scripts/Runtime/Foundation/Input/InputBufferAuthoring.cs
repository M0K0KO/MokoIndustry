using Unity.Entities;
using UnityEngine;

namespace MokoIndustry.Foundation.Input
{
    public class InputBufferAuthoring : MonoBehaviour
    {
        private class Baker : Baker<InputBufferAuthoring>
        {
            public override void Bake(InputBufferAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new InputBufferSingleton());
                AddBuffer<InputBufferElement>(entity);
            }
        }
    }
}