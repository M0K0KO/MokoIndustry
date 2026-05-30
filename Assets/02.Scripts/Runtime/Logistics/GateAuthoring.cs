using MokoIndustry.Foundation;
using MokoIndustry.Foundation.Build;
using MokoIndustry.Foundation.Common;
using Unity.Entities;
using UnityEngine;

namespace MokoIndustry.Logistics
{
    public class GateAuthoring : MonoBehaviour
    {
        public GateMode Mode;
    }

    public class GateAuthoringBaker : Baker<GateAuthoring>
    {
        public override void Bake(GateAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new GateTag());
            AddComponent(entity, new GateSegment
            {
                Mode = authoring.Mode,
                Direction = Direction4.North,
                OutputCooldown = 0,
                LastSideUsed = 0,
                Buffer = default,
            });
            AddComponent(entity, new GridPosition());
            AddComponent(entity, new IOPort());

            AddComponent(entity, new NewlyBuiltTag());
            SetComponentEnabled<NewlyBuiltTag>(entity, true);

            AddComponent(entity, new PendingDestroyTag());
            SetComponentEnabled<PendingDestroyTag>(entity, false);
        }
    }
}