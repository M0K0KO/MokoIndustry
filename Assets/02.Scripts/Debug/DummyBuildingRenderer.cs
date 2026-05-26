using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using MokoIndustry.Foundation.Grid;

namespace MokoIndustry.Foundation.Build
{
    public class DummyBuildingRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject buildingPrefab;  // 단순 큐브

        private Dictionary<Entity, GameObject> _visuals = new();
        private World _world;
        private EntityQuery _buildingQuery;

        private void Start()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            _buildingQuery = _world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<DummyBuildingTag>(),
                ComponentType.ReadOnly<GridPosition>()
            );
        }

        private void LateUpdate()
        {
            if (_world == null || !_world.IsCreated) return;

            var em = _world.EntityManager;

            // 그리드 설정
            var gridQuery = em.CreateEntityQuery(typeof(GridConfigSingleton));
            if (gridQuery.IsEmpty) return;
            var config = gridQuery.GetSingleton<GridConfigSingleton>();

            // 현재 ECS에 있는 모든 Building Entity 수집
            var entities = _buildingQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var currentSet = new HashSet<Entity>();

            // 추가/유지
            foreach (var entity in entities)
            {
                currentSet.Add(entity);

                if (!_visuals.ContainsKey(entity))
                {
                    var pos = em.GetComponentData<GridPosition>(entity);
                    var worldPos = GridUtility.CellToWorld(pos.Cell, in config);
                    var go = Instantiate(buildingPrefab,
                        new Vector3(worldPos.x, worldPos.y + 0.5f, worldPos.z),
                        Quaternion.identity, transform);
                    _visuals[entity] = go;
                }
            }

            // ECS에서 사라진 Entity의 GameObject 제거
            var toRemove = new List<Entity>();
            foreach (var kv in _visuals)
            {
                if (!currentSet.Contains(kv.Key))
                {
                    Destroy(kv.Value);
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var e in toRemove) _visuals.Remove(e);

            entities.Dispose();
        }
    }
}