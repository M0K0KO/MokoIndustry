using Unity.Entities;

namespace MokoIndustry.Foundation
{
    public struct PrefabRegistrySingleton : IComponentData
    {
        public Entity DummyBuildingPrefab;
        public Entity BeltPrefab;
        public Entity RouterPrefab;
        public Entity MinerPrefab;
        public Entity SmelterPrefab;
        public Entity AssemblerPrefab;
        public Entity ItemRendererPrefab;
    }
}