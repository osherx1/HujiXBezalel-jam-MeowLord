using System.Collections.Generic;
using Game.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Click Settings")]
    [SerializeField] private LayerMask clickableLayer;   
    
    [Header("Trail Settings")]
    [SerializeField] private Material lineMaterial;       // Assign a simple unlit/red material in the Inspector
    [SerializeField] private float lineWidth = 0.1f;      // Thickness of the trail
    [Tooltip("Sorting Layer name for the LineRenderer")]
    [SerializeField] private string sortingLayerName;
    
    // remember each click‐point
    private List<Vector2> points = new List<Vector2>();
    // and each LineRenderer GameObject
    private List<GameObject> trails = new List<GameObject>();
    
    
    private Camera _mainCam;
    private GameInput _controls;
    private InputAction _clickAction;
    private InputAction _pointAction;

    void Awake()
    {
        _mainCam      = Camera.main;
        _controls     = InputSystemSingleton.Instance.InputSystem;
        _clickAction  = _controls.PlayerControls.Click;
        _pointAction  = _controls.PlayerControls.Point;
    }

    void OnEnable()
    {
        _clickAction.performed += OnClick;
        _controls.Enable();
    }

    void OnDisable()
    {
        _clickAction.performed -= OnClick;
        _controls.Disable();
    }

    private void OnClick(InputAction.CallbackContext ctx)
    {
        Vector2 screenPos = _pointAction.ReadValue<Vector2>();
        Vector2 worldPos  = _mainCam.ScreenToWorldPoint(screenPos);

        // Determine the clicked point
        Vector2 targetPos;
        var hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, clickableLayer);
        if (hit.collider != null)
            targetPos = hit.point;
        else
            targetPos = worldPos;

        // Draw the red trail
        DrawTrail(transform.position, targetPos);

        // Teleport
        transform.position = targetPos;
    }

    private void DrawTrail(Vector3 from, Vector3 to)
    {
        // Create a new GameObject for this trail segment
        GameObject lineObj = new GameObject("ClickTrail");
        var lr = lineObj.AddComponent<LineRenderer>();

        // Basic setup
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.startWidth = lineWidth;
        lr.endWidth   = lineWidth;
        lr.material    = lineMaterial;
        lr.startColor  = Color.red;
        lr.endColor    = Color.red;
        lr.numCapVertices = 4;  // rounded ends
        lr.sortingLayerName = sortingLayerName;
    }
    
    
    private void ClearAllTrails()
    {
        // destroy all trail GameObjects
        foreach (var go in trails)
            if (go) Destroy(go);

        trails.Clear();
        points.Clear();
    }
}