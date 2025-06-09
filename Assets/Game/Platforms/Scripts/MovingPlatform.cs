using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Platforms.Scripts
{
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Movement Settings")] public float moveSpeed = 2f;

        [Header("Platform State")] [SerializeField]
        private bool _isMoving = false;

        [Header("Animator")] [SerializeField]
        private Animator animator;
        
        [Header("Platform state logic")]
        public bool hasPlayerOnTop = false;  
        public bool hasYarnAttached = false; 
        
        [SerializeField] private Transform spriteRoot;
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
        
        
        private static readonly Vector2 RIGHT_REF = new Vector2(1, -0.5f).normalized;
        private static readonly Vector2 LEFT_REF  = new Vector2(-1, 0.5f).normalized;
        private static readonly Vector2 UP_REF    = new Vector2(1, 0.5f).normalized;
        private static readonly Vector2 DOWN_REF  = new Vector2(-1, -0.5f).normalized;
        
        private enum IsoDirection4 { Right, Left, Up, Down }

        private System.Action<MovingPlatform> _onFinish; // Callback for pool return

        // Init now receives the selected route
        public void Init(GameObject route, float speed, System.Action<MovingPlatform> onFinish)
        {
            routeParent = route;
            moveSpeed = speed;
            _onFinish = onFinish;

            // Collect waypoints from routeParent's children
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
            var polygonCollider = GetComponent<PolygonCollider2D>();
            polygonCollider.enabled = true;
            gameObject.SetActive(true);
        }
        
        void Awake()
        {
            animator = GetComponentInChildren<Animator>();
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
        
        private void HandleRunAway()
        {
            if (animator != null)
                animator.SetBool("IsWalking", true);

            Vector3 startPos = waypoints[0].transform.position;
            transform.position = Vector3.MoveTowards(transform.position, startPos, moveSpeed * Time.deltaTime);

            // Flip sprite
            Vector3 escapeDir = startPos - transform.position;
            UpdateSpriteDirection(escapeDir);

            if (Vector3.Distance(transform.position, startPos) < 0.01f)
            {
                moveSpeed = _savedSpeed;
                _runAway = false;
                _isMoving = false;
                if (animator != null)
                    animator.SetBool("IsWalking", false);
                _onFinish?.Invoke(this);
            }
        }

        
        private bool HandleWaiting()
        {
            if (_waiting)
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                    _waiting = false;
                else
                {
                    if (animator != null)
                        animator.SetBool("IsWalking", false);
                    return true;
                }
            }
            return false;
        }
        
        private void UpdateWalkingAnim()
        {
            if (animator != null)
                animator.SetBool("IsWalking", _isMoving && !_waiting);
        }

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
                    _waiting = true;
                    _waitTimer = waypoints[_currentWaypoint].stopDelay;
                }

                if (_direction == 1 && _currentWaypoint == waypoints.Count - 1)
                {
                    _direction = -1;
                }
                else if (_direction == -1 && _currentWaypoint == 0)
                {
                    // Do not return to pool if player is on top or yarn is attached
                    if (hasPlayerOnTop || hasYarnAttached)
                    {
                        _direction = 1;
                        if (waypoints.Count > 1)
                        {
                            Vector3 nextDir = waypoints[_currentWaypoint + _direction].transform.position - waypoints[_currentWaypoint].transform.position;
                            UpdateSpriteDirection(nextDir);
                        }
                        _isMoving = true;
                        return;
                    }

                    _isMoving = false;
                    if (animator != null)
                        animator.SetBool("IsWalking", false);
                    _onFinish?.Invoke(this);
                    return;
                }

                int nextWaypoint = _currentWaypoint + _direction;
                if (nextWaypoint >= 0 && nextWaypoint < waypoints.Count)
                {
                    Vector3 nextDir = waypoints[nextWaypoint].transform.position - waypoints[_currentWaypoint].transform.position;
                    UpdateSpriteDirection(nextDir);
                }

                _currentWaypoint += _direction;
            }
        }
        
        public void SetAfraid(bool isAfraid)
        {
            if (animator != null)
                animator.SetTrigger("IsAfraid");

            var polygonCollider = GetComponent<PolygonCollider2D>();
            if (polygonCollider != null)
                polygonCollider.enabled = false;

            if (isAfraid)
            {
                if (waypoints != null && waypoints.Count > 0)
                {
                    _savedSpeed = moveSpeed;
                    moveSpeed *= 2f; // Double speed
                    
                    if (_direction != -1)
                        _direction = -1;
                }
            }
            else
            {
                if (_savedSpeed > 0f)
                    moveSpeed = _savedSpeed;
            }
        }

        private void UpdateSpriteDirection(Vector3 direction)
        {
            if (direction == Vector3.zero) return;

            IsoDirection4 dir = GetClosestDirection(new Vector2(direction.x, direction.y));
            Transform flipTarget = spriteRoot != null ? spriteRoot : transform;

            switch (dir)
            {
                case IsoDirection4.Right:
                    flipTarget.localScale = new Vector3(Mathf.Abs(flipTarget.localScale.x), flipTarget.localScale.y, flipTarget.localScale.z);
                    // animator.Play("WalkRight");
                    break;
                case IsoDirection4.Left:
                    flipTarget.localScale = new Vector3(-Mathf.Abs(flipTarget.localScale.x), flipTarget.localScale.y, flipTarget.localScale.z);
                    // animator.Play("WalkLeft");
                    break;
                case IsoDirection4.Up:
                case IsoDirection4.Down:
                    if (direction.x > 0.01f)
                        flipTarget.localScale = new Vector3(Mathf.Abs(flipTarget.localScale.x), flipTarget.localScale.y, flipTarget.localScale.z);
                    else if (direction.x < -0.01f)
                        flipTarget.localScale = new Vector3(-Mathf.Abs(flipTarget.localScale.x), flipTarget.localScale.y, flipTarget.localScale.z);
                    // animator.Play(dir == IsoDirection4.Up ? "WalkUp" : "WalkDown");
                    break;
            }
        }


        public void PlatformReturn()
        {
            OnPlatformReturn?.Invoke();
        }

        private IsoDirection4 GetClosestDirection(Vector2 dir)
        {
            Vector2 right = IsometricDirectionHelper.RightDirection;
            Vector2 left  = IsometricDirectionHelper.LeftDirection;
            Vector2 up    = IsometricDirectionHelper.UpDirection;
            Vector2 down  = IsometricDirectionHelper.DownDirection;

            float dotRight = Vector2.Dot(dir, right);
            float dotLeft  = Vector2.Dot(dir, left);
            float dotUp    = Vector2.Dot(dir, up);
            float dotDown  = Vector2.Dot(dir, down);

            float maxDot = dotRight;
            IsoDirection4 best = IsoDirection4.Right;

            if (dotLeft > maxDot)  { maxDot = dotLeft;  best = IsoDirection4.Left; }
            if (dotUp   > maxDot)  { maxDot = dotUp;    best = IsoDirection4.Up; }
            if (dotDown > maxDot)  { maxDot = dotDown;  best = IsoDirection4.Down; }

            return best;
        }


    }
}
