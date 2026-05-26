using MokoIndustry.Foundation.Common;
using MokoIndustry.Foundation.Grid;
using MokoIndustry.Foundation.Tick;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MokoIndustry.Foundation.Input
{
    public class InputCollector : MonoBehaviour
    {
        [SerializeField] private int inputDelay = 0;

        private Camera _mainCamera;

        private World _world;
        private EntityManager _em;

        private EntityQuery _tickQuery;
        private EntityQuery _bufferQuery;
        private EntityQuery _gridQuery;
        private bool _queriesCreated;

        private Entity _inputBufferEntity;

        private bool _ready;
        
        private Coroutine _initRoutine;

        private PlayerInput _inputActions;

        private BuildingType currentBuilding = BuildingType.Dummy;
        private Direction4 currentDirection = Direction4.East;

        private void Awake()
        {
            _inputActions = new PlayerInput();

            _inputActions.Gameplay.Build.performed += OnBuildPerformed;
            _inputActions.Gameplay.Demolish.performed += OnDemolishPerformed;

            _inputActions.Gameplay.SelectDummy.performed += OnSelectDummyPerformed;
            _inputActions.Gameplay.SelectBelt.performed += OnSelectBeltPerformed;
            _inputActions.Gameplay.Rotate.performed += OnRotatePerformed;
        }

        private void OnEnable()
        {
            _inputActions.Gameplay.Enable();

            if (Application.isPlaying)
                _initRoutine = StartCoroutine(InitializeWhenReady());
        }

        private void OnDisable()
        {
            if (_inputActions != null)
                _inputActions.Gameplay.Disable();

            if (_initRoutine != null)
            {
                StopCoroutine(_initRoutine);
                _initRoutine = null;
            }

            if (_queriesCreated)
            {
                _tickQuery.Dispose();
                _bufferQuery.Dispose();
                _gridQuery.Dispose();

                _queriesCreated = false;
            }

            _ready = false;
        }

        private void OnDestroy()
        {
            if (_inputActions != null)
            {
                _inputActions.Gameplay.Build.performed -= OnBuildPerformed;
                _inputActions.Gameplay.Demolish.performed -= OnDemolishPerformed;

                _inputActions.Gameplay.SelectDummy.performed -= OnSelectDummyPerformed;
                _inputActions.Gameplay.SelectBelt.performed -= OnSelectBeltPerformed;
                _inputActions.Gameplay.Rotate.performed -= OnRotatePerformed;

                _inputActions.Dispose();
                _inputActions = null;
            }
        }

        private IEnumerator InitializeWhenReady()
        {
            _mainCamera = Camera.main;

            while (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                yield return null;
            }

            while (World.DefaultGameObjectInjectionWorld == null ||
                   !World.DefaultGameObjectInjectionWorld.IsCreated)
            {
                yield return null;
            }

            _world = World.DefaultGameObjectInjectionWorld;
            _em = _world.EntityManager;

            // cache all the queries
            _tickQuery = _em.CreateEntityQuery(typeof(TickSingleton));
            _bufferQuery = _em.CreateEntityQuery(typeof(InputBufferSingleton));
            _gridQuery = _em.CreateEntityQuery(typeof(GridConfigSingleton));

            _queriesCreated = true;

            while (_tickQuery.IsEmptyIgnoreFilter ||
                   _bufferQuery.IsEmptyIgnoreFilter ||
                   _gridQuery.IsEmptyIgnoreFilter)
            {
                yield return null;
            }

            _inputBufferEntity = _bufferQuery.GetSingletonEntity();

            _ready = true;
        }

        private void OnBuildPerformed(InputAction.CallbackContext context)
        {
            EnqueueCommand(CommandType.Build);
        }

        private void OnDemolishPerformed(InputAction.CallbackContext context)
        {
            EnqueueCommand(CommandType.Demolish);
        }

        private void OnSelectDummyPerformed(InputAction.CallbackContext ctx)
            => currentBuilding = BuildingType.Dummy;

        private void OnSelectBeltPerformed(InputAction.CallbackContext ctx)
            => currentBuilding = BuildingType.Belt;

        private void OnRotatePerformed(InputAction.CallbackContext ctx)
            => currentDirection = (Direction4)(((byte)currentDirection + 1) & 0b11);

        private void EnqueueCommand(CommandType type)
        {
            if (!_ready) return;
            if (_world == null || !_world.IsCreated) return;
            if (!_em.Exists(_inputBufferEntity)) return;

            if (!TryGetPointerCell(out int2 cell))
                return;

            var tick = _tickQuery.GetSingleton<TickSingleton>();
            var buffer = _em.GetBuffer<InputBufferElement>(_inputBufferEntity);

            int targetTick = tick.Current + inputDelay + 1;

            buffer.Add(new InputBufferElement
            {
                Command = new InputCommand
                {
                    Type = type,
                    Building = currentBuilding,
                    Direction = currentDirection,
                    PlayerId = 0,
                    TargetTick = targetTick,
                    Cell = cell,
                    BuildableId = 0
                }
            });

            Debug.Log($"[Input] {type} ENQUEUE: " +
                  $"TickAtEnqueue={tick.Current}, " +
                  $"TargetTick={tick.Current + inputDelay + 1}, " +
                  $"Frame={Time.frameCount}");
        }

        private bool TryGetPointerCell(out int2 cell)
        {
            cell = default;

            if (_mainCamera == null)
                return false;

            var config = _gridQuery.GetSingleton<GridConfigSingleton>();

            Vector2 screenPos = _inputActions.Gameplay.PointerPosition.ReadValue<Vector2>();

            var ray = _mainCamera.ScreenPointToRay(screenPos);

            var plane = new Plane(
                Vector3.up,
                new Vector3(0f, config.Origin.y, 0f)
            );

            if (!plane.Raycast(ray, out float dist))
                return false;

            Vector3 hit = ray.GetPoint(dist);
            cell = GridUtility.WorldToCell(hit, config);

            return GridUtility.IsInBounds(cell, config);
        }
    }
}