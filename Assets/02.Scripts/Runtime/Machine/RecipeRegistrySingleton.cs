using Unity.Collections;
using Unity.Entities;

namespace MokoIndustry.Machine
{
    public struct RecipeRegistrySingleton : IComponentData
    {
        public FixedList512Bytes<MachineRecipe> Recipes;

        public MachineRecipe Get(RecipeId id) => Recipes[(int)id];
    }
}