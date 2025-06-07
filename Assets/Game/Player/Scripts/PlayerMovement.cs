using System;
using System.Collections.Generic;
using System.Linq;
using Attributes;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core.Input;
using Game.Core.Managers;
using Game.Core.Utils;
using Game.Enemies.Scripts;
using Game.Platforms.Scripts;

namespace Game.Player.Scripts
{
    public class PlayerMovement : MonoBehaviour
    {
        private static readonly int Fall = Animator.StringToHash("Fall");
        private static readonly int IsHovering = Animator.StringToHash("IsHovering");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int Land = Animator.StringToHash("Land");
        [SerializeField] private PlayerLogger playerLogger;
        [SerializeField] private PlayerStats playerStats;
        [Header("Click Settings")] [SerializeField]
        private LayerMask clickableLayer;

        [SerializeField] private LayerMask enemyLayer;

        [Header("Trail Settings")] 
        [SerializeField] GameObject trailRenderer;
        [SerializeField] private float loopDestructionDelay = 0.2f;
        [SerializeField] [Range(1, 20)] private int maxSegments;
        [SerializeField, ReadOnly] private int leftSegments;
        [SerializeField] private Transform segmentsFather;
        public List<Transform> PlayerPlatforms => _visited;

        private List<SegmentCreator.TrailSegment> _segments = new List<SegmentCreator.TrailSegment>();
        private List<Transform> _visited = new List<Transform>();
        private Transform _lastPlat;
        private Camera _mainCam;
        private InputAction _clickAction;
        private MouseSensor _lastPlatScript;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private float moveDuration = 1.25f;

        private bool isMoving = false;
        private Action onMoveComplete;

        private PlayerRadar _playerRadar;

        void Awake()
        {
            leftSegments = maxSegments;
            _clickAction = InputSystemSingleton.Instance.InputSystem.PlayerControls.Click;
            _playerRadar = new PlayerRadar(transform, playerStats, playerLogger,this);
        }

        void Start()
        {
            var (nearest, sensor) = FindNearestPlatformer();

            // Register and snap onto it if found
            RegisterToPlatform(sensor);
            MovePlayerToPlatform(nearest);
            _mainCam = Camera.main;
            GameEvents.NumberOfSegmentsChanged(leftSegments);
            // Find the nearest platform and its sensor
        }
    
        void OnEnable()
        {
            _clickAction.performed += OnClick;
            GameEvents.OnPlayerFall += HandlePlayerFall;
        }

        private void HandlePlayerFall()
        {
            animator.SetTrigger(Fall);
            GameEvents.PlayerFallPointsUpdate(transform.position);
            DOVirtual.DelayedCall(fallDuration, () => {
            // 1. Remove all segment lines
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                RemoveSegment(i);
            }

            // 2. Unregister from all MovingPlatform return events
            foreach (var plat in _visited)
            {
                UnregisterMovingPlatformEvent(plat);
            }
            _platformReturnDelegates.Clear();

            // 3. Unregister from OnPlatformDown of the last platform
            if (_lastPlatScript != null)
            {
                _lastPlatScript.OnPlatformDown -= PlayerFall;
                _lastPlatScript = null;
            }

            // 4. Clear the platforms list
            _visited.Clear();

            // 5. Find nearest platform and restart trail
            var (nearest, sensor) = FindNearestPlatformer();
            RegisterToPlatform(sensor);
            MovePlayerToPlatform(nearest);
            // Optionally log or trigger events
                playerLogger?.Log("Player fell: reset trail and snapped to nearest platform.");
            });
        }


        void OnDisable()
        {
            _clickAction.performed -= OnClick;
            GameEvents.OnPlayerFall -= HandlePlayerFall;
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

        private void MovePlayerToPlatform(Transform platform, bool withDelay = true)
        {
            if (platform != null)
            {
                _lastPlat = platform;
                if (_visited.Count == 0)
                {
                    _visited.Add(platform);
                }
                else if (_visited[_visited.Count - 1] != platform)
                {
                    _visited.Add(platform);
                    RegisterSensorPlatformEvent(platform);
                    playerLogger.Log($"Player {_visited.Count} moving to {platform.name}");
                }
                // Rotate player towards the platform
                RotatePlayerTowards(platform);
                // Animate movement
                DOTween.Kill(transform); // Kill any previous tweens on this transform
                isMoving = true;
                transform.DOMove(platform.position, moveDuration)
                    .SetEase(Ease.Linear)
                    .OnComplete(() => OnMoveComplete(platform));
                playerLogger?.Log("Player Activated event PlayerMoved");
                GameEvents.PlayerMoved();
            }
        }

        private void RotatePlayerTowards(Transform target)
        {
            if (target == null) return;
            float direction = target.position.x - transform.position.x;
            if (Mathf.Abs(direction) > 0.01f)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Sign(direction) * Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }

        private void OnMoveComplete(Transform platform)
        {
            animator.SetTrigger(Land);
            isMoving = false;
            GameEvents.PlayerLanded();
            if (platform.GetComponentInChildren<MovingPlatform>() != null)
                GameEvents.PlayerMovingPlatform(this);
            playerLogger?.Log("Player Activated event PlayerLanded");
            onMoveComplete?.Invoke();
            onMoveComplete = null;
        }

        private void PlayerFall()
        {
            GameEvents.PlayerFall();
        }

        private (Transform, MouseSensor) FindNearestPlatformer()
        {
            float searchRadius = 40f;
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
                    sensor = c.GetComponentInChildren<MouseSensor>();
                }
            }

            return (nearest, sensor);
        }


        private void OnClick(InputAction.CallbackContext ctx)
        {
            if (onMoveComplete != null) return;
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = _mainCam.ScreenToWorldPoint(screenPos);
            var hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, clickableLayer);
            if (hit.collider == null) return;
            if(!_playerRadar.IsPlatformInRange(hit.collider.gameObject)) return;
            var newPlat = hit.collider.transform;
            var newPlatScript = newPlat.GetComponentInChildren<MouseSensor>();
            var prevPlat = _lastPlat;

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
                animator.SetTrigger(Jump);
                return;
            }

            if (_segments.Count >= maxSegments)
            {
                return;
            }
            CreateNewSegment(prevPlat, transform);
            // Case 2: Closing a loop (not immediately previous)
            if (_visited.Contains(newPlat))
            {
                int idx = _visited.IndexOf(newPlat);

                // 1) snapshot exactly the loop of platforms
                List<Transform> loopPlatforms = _visited.GetRange(idx, _visited.Count - idx);

                // 2) immediately prune your history
                for (int i = idx + 1; i < _visited.Count; i++)
                {
                    UnregisterMovingPlatformEvent(_visited[i]);
                }
                _visited.RemoveRange(idx + 1, _visited.Count - idx - 1);

                // 3) schedule the segment creation, LineRenderer cleanup, and enemy destruction after movement
                
                onMoveComplete = () => {
                    DOVirtual.DelayedCall(loopDestructionDelay, () => {
                        for (int i = _segments.Count - 1; i >= idx; i--)
                        {
                            RemoveSegment(i);
                        }
                        DestroyEnemiesInLoop(loopPlatforms);
                    });
                };

                RegisterToPlatform(newPlatScript); // Register before move
                MovePlayerToPlatform(newPlat);
                animator.SetTrigger(Jump);
                return;
            }

            // Case 3: Normal move to new platform
            
            onMoveComplete = () => _segments[^1].ToT = _lastPlat;

            RegisterToPlatform(newPlatScript); // Register before move
            MovePlayerToPlatform(newPlat);
            animator.SetTrigger(Jump);
        }

        private void RemoveSegment(int i)
        {
            Destroy(_segments[i].Lr.gameObject);
            _segments.RemoveAt(i);
            leftSegments = maxSegments - _segments.Count;
            GameEvents.NumberOfSegmentsChanged(leftSegments);
            playerLogger.Log("Removed Segment Line");
        }

        private void CreateNewSegment(Transform fromPlat, Transform toPlat)
        {
            _segments.Add(SegmentCreator.CreateSegment(trailRenderer,segmentsFather,fromPlat.gameObject, toPlat.gameObject));
            leftSegments = maxSegments - _segments.Count;
            GameEvents.NumberOfSegmentsChanged(leftSegments);
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
                {
                    RatHealth ratHealth = c.GetComponent<RatHealth>();
                    if (ratHealth != null)
                    {
                        ratHealth.TakeDamage(1); // Or however much damage the cat deals
                    }
                }
            }
        }
        
        private Dictionary<Transform, Action> _platformReturnDelegates = new();
        [SerializeField] private float fallDuration;


        private void RegisterSensorPlatformEvent(Transform plat)
        {
            var moving = plat.GetComponentInChildren<MovingPlatform>();
            if (moving != null && !_platformReturnDelegates.ContainsKey(plat))
            {
                Action handler = () => OnPlatformReturnHandler(plat);
                moving.OnPlatformReturn += handler;
                _platformReturnDelegates[plat] = handler;
            }
        }

        private void UnregisterMovingPlatformEvent(Transform plat)
        {
            var moving = plat.GetComponentInChildren<MovingPlatform>();
            if (moving != null && _platformReturnDelegates.ContainsKey(plat))
            {
                moving.OnPlatformReturn -= _platformReturnDelegates[plat];
                _platformReturnDelegates.Remove(plat);
            }
        }


        void LateUpdate()
        {
            UpdateSegmentLinePositions();
            if (_lastPlat != null && !isMoving)
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
        
        // ReSharper disable Unity.PerformanceAnalysis
        private void OnPlatformReturnHandler(Transform platform)
        {
            // Find index in _visited
            int idx = _visited.IndexOf(platform);
            if (idx == -1) return; // Not in list
            if (idx == _visited.Count - 1)
            {
                GameEvents.PlayerFall();
                return;
            }
            // Unregister from all platforms being removed (from 0 to idx inclusive)
            for (int i = 0; i <= idx; i++)
            {
                UnregisterMovingPlatformEvent(_visited[i]);
            }

            // Remove segments (segments connect _visited[i] to _visited[i+1])
            // So remove the first 'idx' segments (segment 0 to idx-1)
            for (int i = idx; i >= 0; i--)
            {
                RemoveSegment(i);
            }

            // Remove platforms from 0 to idx inclusive
            _visited.RemoveRange(0, idx + 1);
        }

        private void Update()
        {
            CheckIfMouseOnPlatform();
        }

        private void CheckIfMouseOnPlatform()
        {
            if (onMoveComplete != null) return;
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = _mainCam.ScreenToWorldPoint(screenPos);
            var hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, clickableLayer);
            bool isHovering = hit.collider != null;
            if(hit.transform == _lastPlat)
                isHovering = false;
            if (hit.collider != null && !_playerRadar.IsPlatformInRange(hit.collider.gameObject))
            {
                isHovering = false;
            }
            animator.SetBool(IsHovering, isHovering);
        }
    }
}
