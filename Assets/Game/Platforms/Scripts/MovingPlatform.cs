using System;
using System.Collections.Generic;
using Game.Core.Managers;
using Spine.Unity;
using UnityEngine;

namespace Game.Platforms.Scripts
{
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Movement Settings")] public float moveSpeed = 2f;

        [Header("Platform State")] [SerializeField]
        private bool _isMoving = false;

        [Header("Animators & Sprite Roots")] [SerializeField]
        private Animator animatorForward;

        [SerializeField] private Animator animatorBackward;
        [SerializeField] private Transform spriteRootForward;
        [SerializeField] private Transform spriteRootBackward;

        [Header("References for Layer Changing")]
        public SpriteRenderer forwardSpriteRenderer;

        public SpriteRenderer backwardSpriteRenderer;
        public SkeletonMecanim forwardSkeletonMecanim;
        public SkeletonMecanim backwardSkeletonMecanim;


        // Which direction are we facing right now?
        private bool _isFacingForward = true;
        public bool IsFacingForward => _isFacingForward;

        [Header("Platform state logic")] public bool hasPlayerOnTop = false;
        public bool hasYarnAttached = false;

        public bool isMoving => _isMoving;
        public event Action OnPlatformReturn;

        public PlatformType platformType;

        private GameObject routeParent;
        private List<PlatformWaypointPoint> waypoints = new List<PlatformWaypointPoint>();
        private int _currentWaypoint = 0;
        private int _direction = 1; // 1 = forward, -1 = backward

        private bool _waiting = false;
        private float _waitTimer = 0f;
        private MouseSensor mouseSensor;
        private bool _runAway = false;
        private float _savedSpeed = 0f;

        private System.Action<MovingPlatform> _onFinish;

        // ---------- INIT ----------
        public void Init(GameObject route, float speed, System.Action<MovingPlatform> onFinish)
        {
            routeParent = route;
            moveSpeed = speed;
            _onFinish = onFinish;

            waypoints.Clear();
            foreach (Transform child in routeParent.transform)
            {
                var point = child.GetComponent<PlatformWaypointPoint>();
                if (point != null)
                    waypoints.Add(point);
            }

            _currentWaypoint = 0;
            _direction = 1;
            _isMoving = waypoints.Count > 1;
            _waiting = false;
            _waitTimer = 0f;
            transform.position = waypoints.Count > 0 ? waypoints[0].transform.position : Vector3.zero;

            // Always start with forward visible, backward hidden
            SetSpriteRootActive(true);

            // this line make a bug, there is no polygon collider anymore on the moving platforms.
            // var polygonCollider = GetComponent<PolygonCollider2D>();
            // polygonCollider.enabled = true;

            gameObject.SetActive(true);
        }

        void Awake()
        {
            mouseSensor = GetComponentInChildren<MouseSensor>();
            if (mouseSensor != null)
                mouseSensor.OnAfraidChanged += SetAfraid;
        }

        void Update()
        {
            if (_runAway)
            {
                HandleRunAway();
                return;
            }

            if (!_isMoving) return;
            if (_currentWaypoint < 0 || _currentWaypoint >= waypoints.Count) return;
            if (HandleWaiting()) return;

            UpdateWalkingAnim();

            MoveToWaypoint();

            HandleWaypointArrival();
        }

        void OnDestroy()
        {
            if (mouseSensor != null)
                mouseSensor.OnAfraidChanged -= SetAfraid;
        }

        // ---------- Core movement ----------

        private void MoveToWaypoint()
        {
            Transform currentTransform = waypoints[_currentWaypoint].transform;
            Vector3 targetPos = currentTransform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        }

        private void HandleWaypointArrival()
        {
            Transform currentTransform = waypoints[_currentWaypoint].transform;
            Vector3 targetPos = currentTransform.position;

            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            {
                if (waypoints[_currentWaypoint].stopAtPoint && waypoints[_currentWaypoint].stopDelay > 0f)
                {
                    GameEvents.PlatformStopedMoving();
                    _waiting = true;
                    _waitTimer = waypoints[_currentWaypoint].stopDelay;
                }


                if (_direction == -1 && _currentWaypoint == 0)
                {
                    if (hasPlayerOnTop || hasYarnAttached)
                    {
                        _direction = 1;
                        if (waypoints.Count > 1)
                        {
                            Vector3 nextDir = waypoints[_currentWaypoint + _direction].transform.position -
                                              waypoints[_currentWaypoint].transform.position;
                            UpdateSpriteDirection(nextDir);
                        }

                        _isMoving = true;
                        return;
                    }

                    _isMoving = false;
                    SetWalkingAnim(false);
                    _onFinish?.Invoke(this);
                    return;
                }

                // הפוך לסוף הנתיב קדימה
                if (_direction == 1 && _currentWaypoint == waypoints.Count - 1)
                {
                    _direction = -1;
                }

                int nextWaypoint = _currentWaypoint + _direction;
                if (nextWaypoint >= 0 && nextWaypoint < waypoints.Count)
                {
                    Vector3 nextDir = waypoints[nextWaypoint].transform.position -
                                      waypoints[_currentWaypoint].transform.position;
                    UpdateSpriteDirection(nextDir);
                }

                _currentWaypoint += _direction;
            }
        }

        private bool HandleWaiting()
        {
            if (_waiting)
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                {
                    _waiting = false;
                }
                else
                {
                    SetWalkingAnim(false);
                    return true;
                }
            }

            return false;
        }

        private void UpdateWalkingAnim()
        {
            SetWalkingAnim(_isMoving && !_waiting);
        }

        // ---------- Sprites + Animation Logic ----------

        private void SetWalkingAnim(bool isWalking)
        {
            // Only set the relevant animator
            if (_isFacingForward)
            {
                if (animatorForward != null)
                    animatorForward.SetBool("IsWalking", isWalking);
            }
            else
            {
                if (animatorBackward != null)
                    animatorBackward.SetBool("IsWalking", isWalking);
            }
        }

        private enum IsoDirection4
        {
            RightDown,
            RightUp,
            LeftDown,
            LeftUp,
            Left,
            Up,
            Down
        }

        private void UpdateSpriteDirection(Vector3 direction)
        {
            if (direction == Vector3.zero) return;
            Transform flipTarget = transform;

            var isRight = direction.x > 0;
            var isUp = direction.y > 0;

            SetSpriteRootActive(!isUp);
            var multiplier = isRight ^ isUp ? 1f : -1f;
            flipTarget.localScale = new Vector3(Mathf.Abs(flipTarget.localScale.x) * multiplier,
                flipTarget.localScale.y, flipTarget.localScale.z);
        }

        private void SetSpriteRootActive(bool isForward)
        {
            spriteRootForward.gameObject.SetActive(isForward);
            spriteRootBackward.gameObject.SetActive(!isForward);
        }

        public void OnPlayerJumpedOnKingOrQueen()
        {
            PolygonCollider2D childCollider = null;
            if (_isFacingForward && spriteRootForward != null)
                childCollider = spriteRootForward.GetComponent<PolygonCollider2D>();
            else if (!_isFacingForward && spriteRootBackward != null)
                childCollider = spriteRootBackward.GetComponent<PolygonCollider2D>();
            if (childCollider != null)
                childCollider.enabled = false;

            // animatorForward.SetTrigger("KingHurt");
            // animatorBackward.SetTrigger("KingHurt");

            GoBackToStart();
        }

        private void GoBackToStart()
        {
            _runAway = true;
            _savedSpeed = moveSpeed;
            moveSpeed *= 2f;
            _direction = -1;
            SetWalkingAnim(true);
        }

        private void HandleRunAway()
        {
            Vector3 startPos = waypoints[0].transform.position;
            transform.position = Vector3.MoveTowards(transform.position, startPos, moveSpeed * Time.deltaTime);

            Vector3 escapeDir = startPos - transform.position;
            UpdateSpriteDirection(escapeDir);

            if (Vector3.Distance(transform.position, startPos) < 0.01f)
            {
                moveSpeed = _savedSpeed;
                _runAway = false;
                _isMoving = false;
                SetWalkingAnim(false);
                _onFinish?.Invoke(this);
            }
        }


        // ---------- Afraid/Mouse ----------

        public void SetAfraid(bool isAfraid)
        {
            if (_isFacingForward)
                animatorForward?.SetTrigger("IsAfraid");
            else
                animatorBackward?.SetTrigger("IsAfraid");

            var polygonCollider = GetComponent<PolygonCollider2D>();
            if (polygonCollider != null)
                polygonCollider.enabled = false;

            if (isAfraid)
            {
                if (waypoints != null && waypoints.Count > 0)
                {
                    _savedSpeed = moveSpeed;
                    moveSpeed *= 2f;
                    _direction = -1;
                }
            }
            else
            {
                if (_savedSpeed > 0f)
                    moveSpeed = _savedSpeed;
            }
        }

        // ---------- Utility ----------

        public void PlatformReturn() => OnPlatformReturn?.Invoke();


        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log(
                $"[OnTriggerEnter2D] Triggered by: {other.gameObject.name}, Tag: {other.tag}, IsTrigger: {other.isTrigger}");

            if (!other.isTrigger || !other.CompareTag("BackCollider"))
                return;

            // Forwards SpriteRenderer
            if (forwardSpriteRenderer != null)
            {
                string nextLayer = forwardSpriteRenderer.sortingLayerName != "Platform" ? "Platform" : "Background";
                Debug.Log(
                    $"[OnTriggerEnter2D] Changing forwardSpriteRenderer sortingLayerName from {forwardSpriteRenderer.sortingLayerName} to {nextLayer}");
                forwardSpriteRenderer.sortingLayerName = nextLayer;
            }

            // Backwards SpriteRenderer
            if (backwardSpriteRenderer != null)
            {
                string nextLayer = backwardSpriteRenderer.sortingLayerName != "Platform" ? "Platform" : "Background";
                Debug.Log(
                    $"[OnTriggerEnter2D] Changing backwardSpriteRenderer sortingLayerName from {backwardSpriteRenderer.sortingLayerName} to {nextLayer}");
                backwardSpriteRenderer.sortingLayerName = nextLayer;
            }

            // Forwards SkeletonMecanim
            if (forwardSkeletonMecanim != null)
            {
                var meshRenderer = forwardSkeletonMecanim.GetComponent<Renderer>();
                if (meshRenderer != null)
                {
                    string current = meshRenderer.sortingLayerName;
                    string nextLayer = current != "Platform" ? "Platform" : "Background";
                    Debug.Log(
                        $"[OnTriggerEnter2D] Changing forwardSkeletonMecanim (Renderer) sortingLayerName from {current} to {nextLayer}");
                    meshRenderer.sortingLayerName = nextLayer;
                }
                else
                {
                    Debug.LogWarning("[OnTriggerEnter2D] forwardSkeletonMecanim has no Renderer attached!");
                }
            }

            // Backwards SkeletonMecanim
            if (backwardSkeletonMecanim != null)
            {
                var meshRenderer = backwardSkeletonMecanim.GetComponent<Renderer>();
                if (meshRenderer != null)
                {
                    string current = meshRenderer.sortingLayerName;
                    string nextLayer = current != "Platform" ? "Platform" : "Background";
                    Debug.Log(
                        $"[OnTriggerEnter2D] Changing backwardSkeletonMecanim (Renderer) sortingLayerName from {current} to {nextLayer}");
                    meshRenderer.sortingLayerName = nextLayer;
                }
                else
                {
                    Debug.LogWarning("[OnTriggerEnter2D] backwardSkeletonMecanim has no Renderer attached!");
                }
            }
        }
    }
}