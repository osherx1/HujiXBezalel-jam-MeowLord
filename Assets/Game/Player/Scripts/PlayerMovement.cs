using System;
using System.Collections;
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
using Unity.VisualScripting;

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

        [SerializeField] private float playerLandedTimer;

        [SerializeField] private LayerMask enemyLayer;

        [Header("Trail Settings")] [SerializeField]
        GameObject trailRenderer;

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

        [Header("Animation")] [SerializeField] private Animator animator;
        [SerializeField] [Range(1F, 40F)] private float moveSpeed;
        [SerializeField] private float fallDuration;

        private bool isMoving = false;
        private Action onMoveCompleteEvent;

        private PlayerRadar _playerRadar;
        private Dictionary<Transform, Action> _platformReturnDelegates = new();
        private Coroutine moveCoroutine;
        private bool _fall = false;
        private PlayerRatDetector _playerRatDetector;
        private MovingPlatform _lastMovingPlatform;
        private bool _segmentDelay = false;
        private bool _pausedGame = false;

        void Awake()
        {
            leftSegments = maxSegments;
            _clickAction = InputSystemSingleton.Instance.InputSystem.PlayerControls.Click;
            _playerRadar = new PlayerRadar(transform, playerStats, playerLogger, this, PlayerPlatforms);
            _playerRatDetector = new PlayerRatDetector(_segments, _visited, segmentsFather,playerStats);
        }

        void Start()
        {
            var (nearest, sensor, movingPlatform) = FindNearestPlatformer();

            // Register and snap onto it if found
            onMoveCompleteEvent = GameEvents.PlayerLanded;
            RegisterToPlatform(sensor, movingPlatform);
            MovePlayerToPlatform(nearest);
            _mainCam = Camera.main;
            GameEvents.NumberOfSegmentsChanged(leftSegments);
            // Find the nearest platform and its sensor
        }

        void OnEnable()
        {
            _clickAction.performed += OnClick;
            GameEvents.OnPlayerFall += HandlePlayerFall;
            GameEvents.OnPlayerPause += ActivatePauseGame;
            GameEvents.OnPlayerResume += ActivateResumeGame;
        }

        private void ActivateResumeGame()
        {
            _pausedGame = false;
        }

        private void ActivatePauseGame()
        {
            _pausedGame = true;
        }

        void OnDisable()
        {
            _clickAction.performed -= OnClick;
            GameEvents.OnPlayerFall -= HandlePlayerFall;
            GameEvents.OnGamePause -= ActivatePauseGame;
            GameEvents.OnGameResume -= ActivateResumeGame;
        }

        private void HandlePlayerFall()
        {
            if (onMoveCompleteEvent != null) return;
            _fall = true;
            animator.SetTrigger(Fall);
            GameEvents.PlayerFallPointsUpdate(transform.position);
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                RemoveSegment(i);
            }
            
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
            onMoveCompleteEvent = () =>
            {
                GameEvents.PlayerLanded();
            };
            
            DOVirtual.DelayedCall(fallDuration, () =>
            {
                var (nearest, sensor, movingPlatform) = FindNearestPlatformer();
                RegisterToPlatform(sensor, movingPlatform);
                MovePlayerToPlatform(nearest);
                playerLogger?.Log("Player fell: reset trail and snapped to nearest platform.");
                _fall = false;
            });
        }

        private void RegisterToPlatform(MouseSensor sensor, MovingPlatform movingPlatform)
        {
            // Unregister from old
            if (_lastPlatScript != null)
                _lastPlatScript.OnPlatformDown -= PlayerFall;
            if (_lastMovingPlatform != null)
                _lastMovingPlatform.hasPlayerOnTop = false;

            // Register to new
            _lastPlatScript = sensor;
            _lastMovingPlatform = movingPlatform;
            if (_lastPlatScript != null)
                _lastPlatScript.OnPlatformDown += PlayerFall;
            if (_lastMovingPlatform != null)
                _lastMovingPlatform.hasPlayerOnTop = true;
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

                if (moveCoroutine != null) StopCoroutine(moveCoroutine);
                moveCoroutine = StartCoroutine(MoveToPlatformCoroutine(platform));
                playerLogger?.Log("Player Activated event PlayerMoved");
                GameEvents.PlayerMoved();
            }
        }

        private IEnumerator MoveToPlatformCoroutine(Transform platform)
        {
            isMoving = true;
            while (Vector3.Distance(transform.position, platform.position) > 0.05f)
            {
                while (_pausedGame)
                    yield return null;
                // Move towards the current platform position
                transform.position =
                    Vector3.MoveTowards(transform.position, platform.position, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = platform.position;
            animator.SetTrigger(Land);
            onMoveCompleteEvent?.Invoke();
            onMoveCompleteEvent = null;
            isMoving = false;
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

        private void PlayerFall()
        {
            GameEvents.PlayerFall();
            GameEvents.ScoreCombinatorReady();
        }

        private (Transform, MouseSensor, MovingPlatform) FindNearestPlatformer()
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
            MovingPlatform movingPlatform = null;
            foreach (var c in all)
            {
                float d = Vector2.Distance(transform.position, c.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = c.transform;
                    nearest = EladsHelperFunctions.GetRootTransformPlatformHead(nearest);
                    sensor = c.GetComponentInChildren<MouseSensor>();
                    movingPlatform = c.GetComponentInChildren<MovingPlatform>();
                }
            }
            return (nearest, sensor, movingPlatform);
        }


        private void OnClick(InputAction.CallbackContext ctx)
        {
            if (isMoving || _fall || _pausedGame) return;
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = _mainCam.ScreenToWorldPoint(screenPos);
            var hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, clickableLayer);
            if (hit.collider == null) return;
            var newPlat = hit.collider.transform;
            newPlat = EladsHelperFunctions.GetRootTransformPlatformHead(newPlat);
            if (!_playerRadar.IsPlatformInRange(newPlat.gameObject)) return;
            var newPlatScript = newPlat.GetComponentInChildren<MouseSensor>();
            var prevPlat = _lastPlat;

            if (newPlat == _lastPlat)
                return; // Ignore clicking on the same platform
            
            // Case 1: Backtracking to previous platform
            if (_visited.Count > 1 && _visited[_visited.Count - 2] == newPlat)
            {
                // Remove last segment and last platform
                onMoveCompleteEvent = () =>
                {
                    RemoveSegment(_segments.Count - 1);
                    GameEvents.PlayerLanded();
                };
                _segments[_segments.Count - 1].ToT = transform;
                _visited.RemoveAt(_visited.Count - 1);
                RegisterToPlatform(newPlatScript, _lastMovingPlatform); // Register before move
                MovePlayerToPlatform(newPlat);
                animator.SetTrigger(Jump);
                return;
            }
            

            CreateNewSegment(prevPlat, transform);
            
            var lastPlatMovingPlatform = newPlat.GetComponentInChildren<MovingPlatform>();
            if (lastPlatMovingPlatform != null && (lastPlatMovingPlatform.platformType == PlatformType.Queen ||
                lastPlatMovingPlatform.platformType == PlatformType.King))
            {
                onMoveCompleteEvent = (() =>
                {
                    GameEvents.PlayerLanded();
                    GameEvents.PlayerFall();
                });
                
                RegisterToPlatform(newPlatScript, _lastMovingPlatform); // Register before move
                MovePlayerToPlatform(newPlat);
                animator.SetTrigger(Jump);
                return;
            }
            
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

                onMoveCompleteEvent = () =>
                {
                    _segmentDelay = true;
                    DOVirtual.DelayedCall(loopDestructionDelay, () =>
                    {
                        for (int i = _segments.Count - 1; i >= idx; i--)
                        {
                            RemoveSegment(i);
                        }
                        _playerRatDetector.DestroyEnemiesInLoop(loopPlatforms, enemyLayer);
                        _segmentDelay = false;
                    });
                    DOVirtual.DelayedCall(playerLandedTimer, () =>
                    {
                        GameEvents.PlayerLanded();
                    });
                };

                RegisterToPlatform(newPlatScript, _lastMovingPlatform); // Register before move
                MovePlayerToPlatform(newPlat);
                animator.SetTrigger(Jump);
                return;
            }
            
            if (_segments.Count >= maxSegments)
            {
                RemoveSegment(0);
                RemoveVisitedPlatformAt(0);
            }

            // Case 3: Normal move to new platform

            onMoveCompleteEvent = () =>
            {
                GameEvents.PlayerLanded();
                _segments[^1].ToT = _lastPlat;
            };

            RegisterToPlatform(newPlatScript, _lastMovingPlatform); // Register before move
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
            _segments.Add(SegmentCreator.CreateSegment(trailRenderer, segmentsFather, fromPlat.gameObject,
                toPlat.gameObject));
            leftSegments = maxSegments - _segments.Count;
            GameEvents.NumberOfSegmentsChanged(leftSegments);
        }


        private void RegisterSensorPlatformEvent(Transform plat)
        {
            var moving = plat.GetComponentInChildren<MovingPlatform>();
            if (moving != null && !_platformReturnDelegates.ContainsKey(plat))
            {
                Action handler = () => OnPlatformReturnHandler(plat);
                moving.OnPlatformReturn += handler;
                _platformReturnDelegates[plat] = handler;
                moving.hasYarnAttached = true;
            }
        }

        private void UnregisterMovingPlatformEvent(Transform plat)
        {
            var moving = plat.GetComponentInChildren<MovingPlatform>();
            if (moving != null && _platformReturnDelegates.ContainsKey(plat))
            {
                moving.OnPlatformReturn -= _platformReturnDelegates[plat];
                _platformReturnDelegates.Remove(plat);
                moving.hasYarnAttached = false;
            }
        }


        void LateUpdate()
        {
            UpdateSegmentLinePositions();
            if (_lastPlat != null && !isMoving && !_fall)
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
            if (isMoving || _fall || _pausedGame) return;
            CheckForClosedPolygons();
            CheckIfMouseOnPlatform();
        }

        private void CheckForClosedPolygons()
        {
            if(_segmentDelay) return;
            var polygons = _playerRatDetector.CheckForClosedPolygons();
            if (polygons == null || polygons.Count == 0) return;
            int segRemoveTo = -1;
            int platRemoveTo = -1;
            HashSet<Collider2D> totalEnemies = new HashSet<Collider2D>();
            foreach (var polygon in polygons)
            {
                var enemies = _playerRatDetector.GetEnemiesInLoop(polygon.polygonPoints, enemyLayer);
                if (enemies == null || !enemies.Any())
                    continue;
                totalEnemies.UnionWith(enemies);
                // Remove segments and platforms up to and including the highest index
                segRemoveTo = Mathf.Max(polygon.highestSegmentIndex, segRemoveTo);
                platRemoveTo = Mathf.Max(polygon.highestIndexPlatform,platRemoveTo);
                if (platRemoveTo == _visited.Count - 1)
                    platRemoveTo = platRemoveTo - 1; // Don't remove the platform the player is on
                
            }
            
            if (platRemoveTo != -1 && segRemoveTo != -1 && totalEnemies.Count != 0)
            {
                foreach (var enemy in totalEnemies)
                {
                    _playerRatDetector.ApplyDamageToRat(enemy);
                }
                GameEvents.ScoreCombinatorReady();
                for (int i = 0; i <= segRemoveTo && _segments.Count > 0; i++)
                {
                    RemoveSegment(0);
                }
                for (int i = 0; i <= platRemoveTo && _visited.Count > 1; i++)
                {
                    RemoveVisitedPlatformAt(0);
                }
                GameEvents.PlayerLanded();
            }
        }

        private void CheckIfMouseOnPlatform()
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = _mainCam.ScreenToWorldPoint(screenPos);
            var hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, clickableLayer);
            bool isHovering = hit.collider != null;
            var triggeredPlat = EladsHelperFunctions.GetRootTransformPlatformHead(hit.transform);
            if (triggeredPlat == _lastPlat)
                isHovering = false;
            if (hit.collider != null && !_playerRadar.IsPlatformInRange(triggeredPlat.gameObject))
            {
                isHovering = false;
            }

            animator.SetBool(IsHovering, isHovering);
            GameEvents.PlatformHover(isHovering);
        }

        private void RemoveVisitedPlatformAt(int index)
        {
            UnregisterMovingPlatformEvent(_visited[index]);
            _visited.RemoveAt(index);
        }
        
        
        
    }
}