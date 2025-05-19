using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlatformWaypoint
{
    public Transform transform;
    public bool stopAtPoint = false;
    public float stopDelay = 0f; // seconds
}

public class MovingPlatform : MonoBehaviour
{
    [Header("Waypoints (Assign Transforms, set stop/delay per point)")]
    public List<PlatformWaypoint> waypoints = new List<PlatformWaypoint>();

    [Header("Movement Settings")]
    public float moveSpeed = 2f;

    [Header("Platform State (Read Only)")]
    [SerializeField] private bool _isMoving = false;
    public bool isMoving => _isMoving;

    private int _currentWaypoint = 0;
    private int _direction = 1; // 1 = forward, -1 = backward

    private bool _waiting = false;
    private float _waitTimer = 0f;

    void Start()
    {
        _isMoving = waypoints != null && waypoints.Count > 1 && AllTransformsAssigned();
    }

    void Update()
    {
        if (!_isMoving) return;
        if (waypoints[_currentWaypoint].transform == null) return;

        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _waiting = false;
            }
            else
            {
                return;
            }
        }

        Vector3 targetPos = waypoints[_currentWaypoint].transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            // Should we stop at this point?
            if (waypoints[_currentWaypoint].stopAtPoint && waypoints[_currentWaypoint].stopDelay > 0f)
            {
                _waiting = true;
                _waitTimer = waypoints[_currentWaypoint].stopDelay;
            }

            // Change direction if at ends
            if (_currentWaypoint == waypoints.Count - 1)
                _direction = -1;
            else if (_currentWaypoint == 0)
                _direction = 1;

            _currentWaypoint += _direction;
        }
    }

    // Utility: Check all transforms are assigned
    private bool AllTransformsAssigned()
    {
        foreach (var wp in waypoints)
            if (wp.transform == null)
                return false;
        return true;
    }
}
