using Unity.Entities;
using UnityEngine;

namespace MokoIndustry.Foundation
{
    public class PrefabRegistryAuthoring : MonoBehaviour
    {
        [Header("Buildable Prefab")]
        [SerializeField] private GameObject dummyBuildingPrefab;
        [SerializeField] private GameObject beltPrefab;

        [Header("Item Prefab")]
        [SerializeField] private GameObject itemRendererPrefab;

        private class Baker : Baker<PrefabRegistryAuthoring>
        {
            public override void Bake(PrefabRegistryAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PrefabRegistrySingleton
                {
                    DummyBuildingPrefab = GetEntity(
                        authoring.dummyBuildingPrefab,
                        TransformUsageFlags.Dynamic),

                    BeltPrefab = GetEntity(
                        authoring.beltPrefab,
                        TransformUsageFlags.Dynamic),

                    ItemRendererPrefab = GetEntity(
                        authoring.itemRendererPrefab, 
                        TransformUsageFlags.Dynamic),
                });
            }
        }
    }
}