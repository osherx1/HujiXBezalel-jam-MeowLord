using System.Collections.Generic;
using System.Linq;
using Attributes;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core.Input;
using Game.Core.Managers;
using Game.Core.Utils;

namespace Game.Player.Scripts
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerLogger playerLogger;
        [Header("Click Settings")] [SerializeField]
        private LayerMask clickableLayer;

        [SerializeField] private LayerMask enemyLayer;

        [Header("Trail Settings")] [SerializeField]
        private Material lineMaterial;

        [SerializeField] private float delayForSegments = 0.2f;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private string sortingLayerName;

        [SerializeField] [Range(1, 20)] private int maxSegments;
        [SerializeField, ReadOnly] private int leftSegments;
        public int LeftSegments => leftSegments;
        public List<Transform> PlayerPlatforms => _visited;

        private List<SegmentCreator.TrailSegment> _segments = new List<SegmentCreator.TrailSegment>();
        private List<Transform> _visited = new List<Transform>();
        private Transform _lastPlat;
        private Camera _mainCam;
        private InputAction _clickAction;
        private MouseSensor _lastPlatScript;

        void Awake()
        {
            leftSegments = maxSegments;
            var (nearest, sensor) = FindNearestPlatformer();

            // Register and snap onto it if found
            RegisterToPlatform(sensor);
            MovePlayerToPlatform(nearest);
            _clickAction = InputSystemSingleton.Instance.InputSystem.PlayerControls.Click;
        }

        void Start()
        {
            _mainCam = Camera.main;
            
            // Find the nearest platform and its sensor
        }

        private void RegisterToPlatform(MouseSensor sensor)
        {
            // Unregister from old
            if (_lastPlatScript != null)
                _lastPlatScript.OnPlatformDown -= PlayerFall;

            // Register to new
            _lastPlatScript = sensor;
            if (_lastPlatScript != null)
                _lastPlatScript.OnPlatformDown += PlayerFall;
        }

        private void MovePlayerToPlatform(Transform platform)
        {
            if (platform != null)
            {
                _lastPlat = platform;
                if (_visited.Count == 0)
                {
                    _visited.Add(platform);
                }
                else if (_visited.Count > 0 && _visited[_visited.Count - 1] != platform)
                {
                    _visited.Add(platform);
                    playerLogger.Log($"Player {_visited.Count} moving to {platform.name}");
                }
                transform.position = platform.position;
                GameEvents.PlayerMoved();
            }
        }

        private void PlayerFall()
        {
            GameEvents.PlayerFall();
        }

        private (Transform, MouseSensor) FindNearestPlatformer()
        {
            float searchRadius = 10f;
            Collider2D[] all = Physics2D.OverlapCircleAll(
                transform.position,
                searchRadius,
                clickableLayer
            );
            float minDist = float.MaxValue;
            Transform nearest = null;
            MouseSensor sensor = null;
            foreach (var c in all)
            {
                float d = Vector2.Distance(transform.position, c.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = c.transform;
                    sensor = c.GetComponent<MouseSensor>();
                }
            }

            return (nearest, sensor);
        }

        void OnEnable() => _clickAction.performed += OnClick;
        void OnDisable() => _clickAction.performed -= OnClick;

        private void OnClick(InputAction.CallbackContext ctx)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = _mainCam.ScreenToWorldPoint(screenPos);
            var hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, clickableLayer);
            if (hit.collider == null) return;

            var newPlat = hit.collider.transform;
            var newPlatScript = newPlat.GetComponent<MouseSensor>();

            if (newPlat == _lastPlat)
                return; // Ignore clicking on the same platform

            // Case 1: Backtracking to previous platform
            if (_visited.Count > 1 && _visited[_visited.Count - 2] == newPlat)
            {
                // Remove last segment and last platform
                RemoveSegment(_segments.Count - 1);
                _visited.RemoveAt(_visited.Count - 1);

                RegisterToPlatform(newPlatScript); // Register before move
                MovePlayerToPlatform(newPlat);
                return;
            }

            if (_segments.Count >= maxSegments)
            {
                return;
            }

            // Case 2: Closing a loop (not immediately previous)
            if (_visited.Contains(newPlat))
            {
                CreateNewSegment(newPlat);

                int idx = _visited.IndexOf(newPlat);

                // 1) snapshot exactly the loop of platforms
                List<Transform> loopPlatforms = _visited.GetRange(idx, _visited.Count - idx);

                // 2) immediately prune your history
                _visited.RemoveRange(idx + 1, _visited.Count - idx - 1);

                // 3) schedule the LineRenderer cleanup
                DOVirtual.DelayedCall(delayForSegments, () =>
                {
                    for (int i = _segments.Count - 1; i >= idx; i--)
                    {
                        RemoveSegment(i);
                    }
                });

                // 4) destroy enemies inside that polygon
                DestroyEnemiesInLoop(loopPlatforms);

                RegisterToPlatform(newPlatScript); // Register before move
                MovePlayerToPlatform(newPlat);
                return;
            }

            // Case 3: Normal move to new platform
            CreateNewSegment(newPlat);

            RegisterToPlatform(newPlatScript); // Register before move
            MovePlayerToPlatform(newPlat);
        }

        private void RemoveSegment(int i)
        {
            Destroy(_segments[i].Lr.gameObject);
            _segments.RemoveAt(i);
            leftSegments = maxSegments - _segments.Count;
            playerLogger.Log("Removed Segment Line");
        }

        private void CreateNewSegment(Transform newPlat)
        {
            
            _segments.Add(SegmentCreator.CreateSegment(_lastPlat.gameObject, newPlat.gameObject,
                lineWidth, lineMaterial, sortingLayerName));
            leftSegments = maxSegments - _segments.Count;
        }

        private void DestroyEnemiesInLoop(List<Transform> loopPlatforms)
        {
            Vector2[] poly = loopPlatforms
                .Select(t => (Vector2)t.position)
                .ToArray();

            float minX = poly.Min(v => v.x), maxX = poly.Max(v => v.x);
            float minY = poly.Min(v => v.y), maxY = poly.Max(v => v.y);
            Vector2 min = new Vector2(minX, minY);
            Vector2 max = new Vector2(maxX, maxY);

            Collider2D[] candidates = Physics2D.OverlapAreaAll(min, max, enemyLayer);

            foreach (var c in candidates)
            {
                Vector2 pt = c.transform.position;
                if (EladsHelperFunctions.PointInPolygon(poly, pt))
                    Destroy(c.gameObject);
            }
        }

        void LateUpdate()
        {
            UpdateSegmentLinePositions();
            if (_lastPlat != null)
                transform.position = _lastPlat.position;
        }

        private void UpdateSegmentLinePositions()
        {
            foreach (var seg in _segments)
            {
                seg.Lr.SetPosition(0, seg.FromT.TransformPoint(seg.FromLocalPos));
                seg.Lr.SetPosition(1, seg.ToT.TransformPoint(seg.ToLocalPos));
            }
        }
    }
}
