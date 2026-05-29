using Unity.Entities;
using UnityEngine;

namespace MokoIndustry.Foundation
{
    public class PrefabRegistryAuthoring : MonoBehaviour
    {
        [Header("Buildable Prefab")]
        [SerializeField] private GameObject dummyBuildingPrefab;
        [SerializeField] private GameObject beltPrefab;
        [SerializeField] private GameObject routerPrefab;
        [SerializeField] private GameObject minerPrefab;
        [SerializeField] private GameObject smelterPrefab;

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

                    RouterPrefab = GetEntity(
                        authoring.routerPrefab,
                        TransformUsageFlags.Dynamic),

                    MinerPrefab = GetEntity(
                        authoring.minerPrefab,
                        TransformUsageFlags.Dynamic),

                    SmelterPrefab = GetEntity(
                        authoring.smelterPrefab,
                        TransformUsageFlags.Dynamic),

                    ItemRendererPrefab = GetEntity(
                        authoring.itemRendererPrefab, 
                        TransformUsageFlags.Dynamic),
                });
            }
        }
    }
}