using MokoIndustry.Foundation.Build;
using MokoIndustry.Foundation.Common;
using Unity.Entities;
using UnityEngine;


namespace MokoIndustry.Logistics
{
    public class RouterAuthoring : MonoBehaviour
    {
        public class Baker : Baker<RouterAuthoring>
        {
            public override void Bake(RouterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<RouterTag>(entity);
                AddComponent(entity, new GridPosition { Cell = default });
                AddComponent(entity, new IOPort
                {
                    InputMask = 0,
                    OutputMask = 0,
                    AcceptFilter = 0
                });

                AddComponent(entity, new RouterSegment
                {
                    Buffer = default,
                    RoundRobinPtr = 0,
                });

                AddComponent<NewlyBuiltTag>(entity);
                SetComponentEnabled<NewlyBuiltTag>(entity, true);

                AddComponent<PendingDestroyTag>(entity);
                SetComponentEnabled<PendingDestroyTag>(entity, false);
            }
        }
    }
}