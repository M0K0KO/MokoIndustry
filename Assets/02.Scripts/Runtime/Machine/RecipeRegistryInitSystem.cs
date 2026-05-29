using MokoIndustry.Belt;
using MokoIndustry.Foundation;
using Unity.Collections;
using Unity.Entities;

namespace MokoIndustry.Machine
{
    [UpdateInGroup(typeof(FoundationSystemGroup), OrderFirst = true)]
    public partial struct RecipeRegistryInitSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            var recipes = new FixedList512Bytes<MachineRecipe>();

            // None placeholder (index 0)
            recipes.Add(default);

            // Mine_Ore: -> 1 Ore, 30 ticks
            var mineOre = new MachineRecipe { ProcessingTicks = 30 };
            mineOre.Outputs.Add(new RecipeItem { ItemId = (byte)ItemId.Ore, Count = 1 });
            recipes.Add(mineOre);

            // Smelt_Plate: 1 Ore -> 1 Plate, 60 ticks
            var smeltPlate = new MachineRecipe { ProcessingTicks = 60 };
            smeltPlate.Inputs.Add(new RecipeItem { ItemId = (byte)ItemId.Ore, Count = 1 });
            smeltPlate.Outputs.Add(new RecipeItem { ItemId = (byte)ItemId.Plate, Count = 1 });
            recipes.Add(smeltPlate);

            // Assemble_Ammo: 2 Plate -> 1 Ammo, 90 ticks
            var assembleAmmo = new MachineRecipe { ProcessingTicks = 90 };
            assembleAmmo.Inputs.Add(new RecipeItem { ItemId = (byte)ItemId.Plate, Count = 2 });
            assembleAmmo.Outputs.Add(new RecipeItem { ItemId = (byte)ItemId.Ammo, Count = 1 });
            recipes.Add(assembleAmmo);

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new RecipeRegistrySingleton { Recipes = recipes });

            state.Enabled = false;
        }
    }
}