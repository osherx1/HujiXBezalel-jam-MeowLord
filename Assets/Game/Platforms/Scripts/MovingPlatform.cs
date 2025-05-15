using UnityEngine;
using System.Collections.Generic;

public class MovingPlatform : MonoBehaviour
{
    [Header("Waypoints (Assign Transforms)")]
    public List<Transform> waypoints = new List<Transform>();

    [Header("Movement Settings")]
    public float moveSpeed = 2f;

    [Header("Platform State (Read Only)")]
    [SerializeField] private bool _isMoving = false;
    public bool isMoving => _isMoving;

    private int _currentWaypoint = 0;
    private int _direction = 1; // 1 = forward, -1 = backward

    void Start()
    {
        _isMoving = waypoints != null && waypoints.Count > 1;
        //if (waypoints == null || waypoints.Count == 0)
            //Debug.LogWarning("MovingPlatform: No waypoints assigned!");
    }

    void Update()
    {
        if (!_isMoving) return;
        if (waypoints[_currentWaypoint] == null) return;

        Vector3 targetPos = waypoints[_currentWaypoint].position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            // At waypoint, change direction if at ends
            if (_currentWaypoint == waypoints.Count - 1)
                _direction = -1;
            else if (_currentWaypoint == 0)
                _direction = 1;

            _currentWaypoint += _direction;
        }
    }
}