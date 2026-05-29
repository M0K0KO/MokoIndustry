using Unity.Collections;
using Unity.Entities;

namespace MokoIndustry.Machine
{
    public struct MachineRecipeRef : IComponentData
    {
        public RecipeId Id;
    }
}