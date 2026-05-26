using Unity.Entities;

namespace MokoIndustry.Foundation
{
    public struct PrefabRegistrySingleton : IComponentData
    {
        public Entity DummyBuildingPrefab;
        public Entity BeltPrefab;
    }
}