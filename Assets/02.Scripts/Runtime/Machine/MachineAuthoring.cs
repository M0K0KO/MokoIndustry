using MokoIndustry.Foundation.Build;
using MokoIndustry.Foundation.Common;
using MokoIndustry.Logistics;
using Unity.Entities;
using UnityEngine;

namespace MokoIndustry.Machine
{
    public class MachineAuthoring : MonoBehaviour
    {
        public RecipeId RecipeId;

        public class MachineAuthoringBaker : Baker<MachineAuthoring>
        {
            public override void Bake(MachineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new MachineTag());
                AddComponent(entity, new MachineRecipeRef { Id = authoring.RecipeId });
                AddComponent(entity, new MachineState());
                AddComponent(entity, new GridPosition());
                AddComponent(entity, new IOPort());

                AddComponent(entity, new NewlyBuiltTag());
                SetComponentEnabled<NewlyBuiltTag>(entity, true);

                AddComponent(entity, new PendingDestroyTag());
                SetComponentEnabled<PendingDestroyTag>(entity, false);
            }
        }
    }
}