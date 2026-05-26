using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MokoIndustry.Foundation.Grid
{
    public class GridGizmoDrawer : MonoBehaviour
    {
        [SerializeField] private bool drawGrid = true;
        [SerializeField] private Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        private bool _hasConfig;
        private GridConfigSingleton _config;

        private Coroutine _waitRoutine;

        private void OnEnable()
        {
            if (Application.isPlaying)
                _waitRoutine = StartCoroutine(WaitForGridConfig());
        }

        private void OnDisable()
        {
            if (_waitRoutine != null)
            {
                StopCoroutine(_waitRoutine);
                _waitRoutine = null;
            }

            _hasConfig = false;
        }

        private IEnumerator WaitForGridConfig()
        {
            while (true)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null)
                {
                    yield return null;
                    continue;
                }

                var em = world.EntityManager;

                using var query = em.CreateEntityQuery(typeof(GridConfigSingleton));

                if (!query.IsEmpty)
                {
                    _config = query.GetSingleton<GridConfigSingleton>();
                    _hasConfig = true;
                    yield break;
                }

                yield return null;
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGrid) return;
            if (!Application.isPlaying) return;
            if (!_hasConfig) return;

            Gizmos.color = gridColor;

            float width = _config.Size.x * _config.CellSize;
            float height = _config.Size.y * _config.CellSize;

            for (int x = 0; x <= _config.Size.x; x++)
            {
                float wx = _config.Origin.x + x * _config.CellSize;
                Gizmos.DrawLine(
                    new Vector3(wx, _config.Origin.y, _config.Origin.z),
                    new Vector3(wx, _config.Origin.y, _config.Origin.z + height)
                );
            }
            for (int y = 0; y <= _config.Size.y; y++)
            {
                float wz = _config.Origin.z + y * _config.CellSize;
                Gizmos.DrawLine(
                    new Vector3(_config.Origin.x, _config.Origin.y, wz),
                    new Vector3(_config.Origin.x + width, _config.Origin.y, wz)
                );
            }
        }
    }
}