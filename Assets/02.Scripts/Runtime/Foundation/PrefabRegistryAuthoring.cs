using Unity.Entities;
using UnityEngine;

namespace MokoIndustry.Foundation
{
    public class PrefabRegistryAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject dummyBuildingPrefab;

        private class Baker : Baker<PrefabRegistryAuthoring>
        {
            public override void Bake(PrefabRegistryAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PrefabRegistrySingleton
                {
                    DummyBuildingPrefab = GetEntity(
                        authoring.dummyBuildingPrefab,
                        TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}