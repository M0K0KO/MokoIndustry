using Unity.Entities;

namespace MokoIndustry.Belt
{
    public struct ItemRendererState : IComponentData
    {
        public Entity OwnerBelt;
        public int ArrayIndex;
    }
}