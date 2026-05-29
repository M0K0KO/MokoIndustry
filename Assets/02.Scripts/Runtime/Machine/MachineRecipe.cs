using Unity.Collections;

namespace MokoIndustry.Machine
{
    public struct RecipeItem
    {
        public byte ItemId;
        public byte Count;
    }

    // possessed by RecipeRegistrySingleton as a LookupTable at runtime
    public struct MachineRecipe
    {
        public FixedList32Bytes<RecipeItem> Inputs;
        public FixedList32Bytes<RecipeItem> Outputs;
        public ushort ProcessingTicks; // 30 = 1sec
    }
}