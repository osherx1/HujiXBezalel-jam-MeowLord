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
        
        [SerializeField] private Transform spriteRoot;
        public bool isMoving => _isMoving;
        public PlatformType platformType;

        private GameObject routeParent;
        private List<PlatformWaypointPoint> waypoints = new List<PlatformWaypointPoint>();
        private int _currentWaypoint = 0;
        private int _direction = 1; // 1 = forward, -1 = backward

        private bool _waiting = false;
        private float _waitTimer = 0f;
        private MouseSensor mouseSensor;

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
            gameObject.SetActive(true);
        }
        
        void Awake()
        {
            animator = GetComponent<Animator>();
            
            mouseSensor = GetComponentInChildren<MouseSensor>();
        }

        void Update()
        {
            if (!_isMoving) return;
            if (_currentWaypoint < 0 || _currentWaypoint >= waypoints.Count) return;
            Transform currentTransform = waypoints[_currentWaypoint].transform;

            if (_waiting)
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                    _waiting = false;
                else
                    return;
            }
            
            if (animator != null)
            {
                animator.SetBool("IsWalking", _isMoving && !_waiting);
            }
            

            Vector3 targetPos = currentTransform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

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
                    // We're back at the start, return to pool before increasing/decreasing _currentWaypoint
                    _isMoving = false;
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
        
        void OnEnable()
        {
            Game.Core.Managers.GameEvents.OnAfraidChanged += SetAfraid;
        }

        void OnDisable()
        {
            Game.Core.Managers.GameEvents.OnAfraidChanged -= SetAfraid;
        }
        
        public void SetAfraid(bool isAfraid)
        {
            if (animator != null)
                animator.SetBool("IsAfraid", isAfraid);
            
            var polygonCollider = GetComponent<PolygonCollider2D>();
            if (polygonCollider != null)
                polygonCollider.enabled = !isAfraid;

            // If afraid, immediately return to start and despawn (return to pool)
            if (isAfraid)
            {
                // Move instantly to start of route
                if (waypoints != null && waypoints.Count > 0)
                    transform.position = waypoints[0].transform.position;

                // Deactivate and return to pool
                _isMoving = false;
                _onFinish?.Invoke(this); // return to pool immediately
            }
        }
        
        private void UpdateSpriteDirection(Vector3 direction)
        {
            if (direction.x == 0) return;

            Transform flipTarget = spriteRoot != null ? spriteRoot : transform;

            float facing = direction.x > 0 ? 1f : -1f;
            Vector3 scale = flipTarget.localScale;
            scale.x = Mathf.Abs(scale.x) * facing;
            flipTarget.localScale = scale;
        }
    }
}
