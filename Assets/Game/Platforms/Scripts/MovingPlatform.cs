using System.Collections.Generic;
using UnityEngine;

namespace Game.Platforms.Scripts
{
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Assign route parent GameObject (with children as points)")]
        public GameObject routeParent;

        [Header("Movement Settings")]
        public float moveSpeed = 2f;

        [Header("Platform State (Read Only)")]
        [SerializeField] private bool _isMoving = false;
        public bool isMoving => _isMoving;
        public PlatformType platformType;

        private List<PlatformWaypointPoint> waypoints = new List<PlatformWaypointPoint>();
        private int _currentWaypoint = 0;
        private int _direction = 1; // 1 = forward, -1 = backward

        private bool _waiting = false;
        private float _waitTimer = 0f;
        
        public void Init(GameObject route, float speed, System.Action<MovingPlatform> onFinish)
        {
            routeParent = route;
            moveSpeed = speed;
            // Assign a callback for when returning to pool, if needed:
            // this._onFinish = onFinish;

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
            // You may save the initial position here if needed
            gameObject.SetActive(true);
        }

        
        void Start()
        {
            // Extract all PlatformWaypointPoint components from the children of routeParent
            if (routeParent == null)
            {
                Debug.LogError("No route parent assigned!");
                _isMoving = false;
                return;
            }

            waypoints.Clear();
            foreach (Transform child in routeParent.transform)
            {
                var point = child.GetComponent<PlatformWaypointPoint>();
                if (point != null)
                    waypoints.Add(point);
            }

            _isMoving = waypoints.Count > 1;
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

            Vector3 targetPos = currentTransform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            {
                if (waypoints[_currentWaypoint].stopAtPoint && waypoints[_currentWaypoint].stopDelay > 0f)
                {
                    _waiting = true;
                    _waitTimer = waypoints[_currentWaypoint].stopDelay;
                }

                if (_currentWaypoint == waypoints.Count - 1)
                    _direction = -1;
                else if (_currentWaypoint == 0)
                    _direction = 1;

                _currentWaypoint += _direction;
            }
        }
    }
}
