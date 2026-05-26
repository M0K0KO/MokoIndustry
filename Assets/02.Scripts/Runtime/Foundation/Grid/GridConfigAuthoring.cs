using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MokoIndustry.Foundation.Grid
{
    public class GridConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private int sizeX = 256;
        [SerializeField] private int sizeY = 256;
        [SerializeField] private float cellSize = 1.0f;
        [SerializeField] private Vector3 origin = Vector3.zero;

        public int2 Size => new int2(sizeX, sizeY);

        private class Baker : Baker<GridConfigAuthoring>
        {
            public override void Bake(GridConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GridConfigSingleton
                {
                    Size = authoring.Size,
                    CellSize = authoring.cellSize,
                    Origin = authoring.origin
                });
            }
        }
    }
}