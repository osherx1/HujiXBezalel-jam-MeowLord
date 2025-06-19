using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Attributes;
using DG.Tweening;
using Game.Core.Audio;
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
        private bool _pausedGame = false;

        [SerializeField] private int[] yarnThresholds = { 200, 1000, 2500, 10000 };
        private int _yarnThresholdIndex = 0;

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
            GameEvents.OnGameStarted += GameStarted;
            GameEvents.OnTutorialStarted += GameStarted;
            GameEvents.OnTutorialReset += GameStarted;
            GameEvents.OnGameFinished += GameFinished;
            GameEvents.OnPlayerPause += ActivatePauseGame;
            GameEvents.OnPlayerResume += ActivateResumeGame;
            GameEvents.OnUpdateScore += CheckYarn;
        }

        private void GameFinished()
        {
            _clickAction.performed -= OnClick;
            GameEvents.OnPlayerFall -= HandlePlayerFall;
            GameEvents.OnUpdateScore -= CheckYarn;
        }

        private void GameStarted()
        {
            _clickAction.performed += OnClick;
            GameEvents.OnPlayerFall += HandlePlayerFall;
        }
        private void GameStarted(Action<Action> action)
        {
            _clickAction.performed += OnClick;
            GameEvents.OnPlayerFall += HandlePlayerFall;
            GameEvents.OnUpdateScore += CheckYarn;
        }

        private void CheckYarn(int score)
        {
            // Increase maxSegments by one for each threshold passed, only once per threshold
            if (_yarnThresholdIndex < yarnThresholds.Length && score >= yarnThresholds[_yarnThresholdIndex])
            {
                AudioManager.Instance.Play(AudioName.Wool, transform.position);
                maxSegments++;
                leftSegments = maxSegments - _segments.Count;
                GameEvents.NumberOfSegmentsChanged(leftSegments);
                _yarnThresholdIndex++;
            }
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
            GameEvents.OnPlayerPause -= ActivatePauseGame;
            GameEvents.OnPlayerResume -= ActivateResumeGame;
            GameEvents.OnUpdateScore -= CheckYarn;
            GameEvents.OnGameStarted -= GameStarted;
            GameEvents.OnGameFinished -= GameFinished;
            GameEvents.OnTutorialStarted -= GameStarted;
            GameEvents.OnTutorialReset -= GameStarted;
            
        }

        private void HandlePlayerFall()
        {
            if (onMoveCompleteEvent != null) return;
            _fall = true;
            animator.SetTrigger(Fall);
            GameEvents.PlayerFallPointsUpdate(transform.position);
            RemoveAllSegments();
            UnregisterAllVisitedPlatforms();
            _platformReturnDelegates.Clear();
            UnregisterLastPlatScript();
            ClearVisitedPlatforms();
            onMoveCompleteEvent = () =>
            {
                AudioManager.Instance.Play(AudioName.CatLand, transform.position);
                GameEvents.PlayerLanded();
            };
            DOVirtual.DelayedCall(fallDuration, ResetPlayerAfterFall);
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
            if (platform == null) return;
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
            RotatePlayerTowards(platform);
            AnimateAndMoveToPlatform(platform);
        }

        private void AnimateAndMoveToPlatform(Transform platform)
        {
            DOTween.Kill(transform); // Kill any previous tweens on this transform
            isMoving = true;
            GameEvents.PlayerMoved();
            AudioManager.Instance.Play(AudioName.CatJump, transform.position);
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveToPlatformCoroutine(platform));
            playerLogger?.Log("Player Activated event PlayerMoved");
            GameEvents.PlayerMoved();
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
                var father = EladsHelperFunctions.GetRootTransformPlatformHead(c.transform);
                var movingPlatfomCheck = father.GetComponentInChildren<MovingPlatform>();
                if (movingPlatfomCheck != null && (movingPlatfomCheck.platformType is PlatformType.King or PlatformType.Queen))
                {
                    continue;
                }
                float d = Vector2.Distance(transform.position, father.position);
                if (d < minDist)
                {
                    movingPlatform = father.GetComponentInChildren<MovingPlatform>();
                    minDist = d;
                    nearest = father.transform;
                    sensor = father.GetComponentInChildren<MouseSensor>();
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
            if (newPlat == _lastPlat) return; // Ignore clicking on the same platform
            if (HandleBacktrack(newPlat, newPlatScript)) return;
            CreateNewSegment(prevPlat, transform);
            if (HandleSpecialPlatform(newPlat, newPlatScript)) return;
            if (HandleLoop(newPlat, newPlatScript)) return;
            HandleNormalMove(newPlat, newPlatScript);
        }

        private void SegmentMaxCheck()
        {
            if (_segments.Count >= maxSegments)
            {
                RemoveSegment(0);
                RemoveVisitedPlatformAt(0);
            }
        }

        private bool HandleBacktrack(Transform newPlat, MouseSensor newPlatScript)
        {
            if (_visited.Count > 1 && _visited[_visited.Count - 2] == newPlat)
            {
                onMoveCompleteEvent = () =>
                {
                    AudioManager.Instance.Play(AudioName.CatLand, transform.position);
                    RemoveSegment(_segments.Count - 1);
                    GameEvents.PlayerLanded();
                };
                _segments[_segments.Count - 1].ToT = transform;
                _visited.RemoveAt(_visited.Count - 1);
                RegisterToPlatform(newPlatScript, _lastMovingPlatform);
                MovePlayerToPlatform(newPlat);
                animator.SetTrigger(Jump);
                return true;
            }
            return false;
        }

        private bool HandleSpecialPlatform(Transform newPlat, MouseSensor newPlatScript)
        {
            var lastPlatMovingPlatform = newPlat.GetComponentInChildren<MovingPlatform>();
            if (lastPlatMovingPlatform != null && (lastPlatMovingPlatform.platformType == PlatformType.Queen ||
                lastPlatMovingPlatform.platformType == PlatformType.King))
            {
                onMoveCompleteEvent = (() =>
                {
                    HandleGameLoss();
                });
                RegisterToPlatform(newPlatScript, _lastMovingPlatform);
                MovePlayerToPlatform(newPlat);
                animator.SetTrigger(Jump);
                return true;
            }
            return false;
        }

        private bool HandleLoop(Transform newPlat, MouseSensor newPlatScript)
        {
            if (_visited.Contains(newPlat))
            {
                int idx = _visited.IndexOf(newPlat);
                List<Transform> loopPlatforms = _visited.GetRange(idx, _visited.Count - idx);
                for (int i = idx + 1; i < _visited.Count; i++)
                {
                    UnregisterMovingPlatformEvent(_visited[i]);
                }
                _visited.RemoveRange(idx + 1, _visited.Count - idx - 1);
                onMoveCompleteEvent = () =>
                {
                    if (_playerRatDetector.HasKingOrQueenInLoopPolygon(loopPlatforms, playerStats.platformLayer))
                    {
                        HandleGameLoss();
                        return;
                    }
                    
                    AudioManager.Instance.Play(AudioName.CatLand, transform.position);
                    if (_playerRatDetector.DestroyEnemiesInLoop(loopPlatforms, enemyLayer))
                    {
                        DOVirtual.DelayedCall(playerLandedTimer/4, () => GameEvents.PlayerLanded());
                    }
                    else
                    {
                        DOVirtual.DelayedCall(playerLandedTimer, () => GameEvents.PlayerLanded());
                    }
                    for (int i = _segments.Count - 1; i >= idx; i--)
                    {
                        RemoveSegment(i);
                    }
                };
                RegisterToPlatform(newPlatScript, _lastMovingPlatform);
                MovePlayerToPlatform(newPlat);
                animator.SetTrigger(Jump);
                return true;
            }
            return false;
        }

        private void HandleNormalMove(Transform newPlat, MouseSensor newPlatScript)
        {
            onMoveCompleteEvent = () =>
            {
                SegmentMaxCheck();
                AudioManager.Instance.Play(AudioName.CatLand, transform.position);
                GameEvents.PlayerLanded();
                _segments[^1].ToT = _lastPlat;
            };
            RegisterToPlatform(newPlatScript, _lastMovingPlatform);
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
            if (_pausedGame) return;
            CheckForClosedPolygons();
            CheckIfMouseOnPlatform();
        }

        private void CheckForClosedPolygons()
        {
            if (isMoving || _fall) return;
            var polygons = _playerRatDetector.CheckForClosedPolygons();
            if (polygons == null || polygons.Count == 0) return;
            int segRemoveTo = -1;
            int platRemoveTo = -1;
            HashSet<Collider2D> totalEnemies = new HashSet<Collider2D>();
            foreach (var polygon in polygons)
            {
                // Check for King or Queen in the polygon
                if (_playerRatDetector.HasKingOrQueenInLoopPolygon(polygon.polygonPoints, playerStats.platformLayer))
                {
                    HandleGameLoss();
                    return;
                }
                
                var enemies = _playerRatDetector.GetEnemiesInLoop(polygon.polygonPoints, enemyLayer);
                if (enemies == null || !enemies.Any())
                    continue;
                totalEnemies.UnionWith(enemies);
                segRemoveTo = Mathf.Max(polygon.highestSegmentIndex, segRemoveTo);
                platRemoveTo = Mathf.Max(polygon.highestIndexPlatform,platRemoveTo);
                if (platRemoveTo == _visited.Count - 1)
                    platRemoveTo = platRemoveTo - 1;
            }
            if (platRemoveTo != -1 && segRemoveTo != -1 && totalEnemies.Count != 0)
            {
                HandlePolygonEnemies(totalEnemies, segRemoveTo, platRemoveTo);
            }
        }

        private void HandleGameLoss()
        {
            AudioManager.Instance.Play(AudioName.CatBadJump, transform.position);
            GameEvents.GameFinished();
        }

        private void HandlePolygonEnemies(HashSet<Collider2D> totalEnemies, int segRemoveTo, int platRemoveTo)
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
            DOVirtual.DelayedCall(playerLandedTimer, () =>
            {
                GameEvents.PlayerLanded();
            });
        }

        private void CheckIfMouseOnPlatform()
        {
            if(isMoving) return;
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

        private void RemoveAllSegments()
        {
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                RemoveSegment(i);
            }
        }

        private void UnregisterAllVisitedPlatforms()
        {
            foreach (var plat in _visited)
            {
                UnregisterMovingPlatformEvent(plat);
            }
        }

        private void UnregisterLastPlatScript()
        {
            if (_lastPlatScript != null)
            {
                _lastPlatScript.OnPlatformDown -= PlayerFall;
                _lastPlatScript = null;
            }
        }

        private void ClearVisitedPlatforms()
        {
            _visited.Clear();
        }

        private void ResetPlayerAfterFall()
        {
            var (nearest, sensor, movingPlatform) = FindNearestPlatformer();
            RegisterToPlatform(sensor, movingPlatform);
            MovePlayerToPlatform(nearest);
            playerLogger?.Log("Player fell: reset trail and snapped to nearest platform.");
            _fall = false;
        }
    }
}