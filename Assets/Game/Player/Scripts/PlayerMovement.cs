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
    
    private Dictionary<GameObject,GameObject> _clickedPlatforms = new Dictionary<GameObject, GameObject>();
    
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
    }

    void OnDisable()
    {
        _clickAction.performed -= OnClick;
    }

    private void OnClick(InputAction.CallbackContext ctx)
    {
        Vector2 screenPos = _pointAction.ReadValue<Vector2>();
        Vector2 worldPos  = _mainCam.ScreenToWorldPoint(screenPos);
        
        var hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, clickableLayer);
        if (hit.collider == null)
            return;
        
        GameObject platform = hit.collider.gameObject;
            
        Vector2 targetPos = platform.transform.position;
        
        HandleNewMovement(transform.position, targetPos, platform);
        
        transform.position = targetPos;
        

    }

    private void HandleNewMovement(Vector3 from, Vector3 to, GameObject platform)
    {
        
        if (_clickedPlatforms.ContainsKey(platform))
        {
            if (!checkForShape())
            {
                DrawTrail(from, to);
            }
        }
        
    }

    private void DrawTrail(Vector3 from, Vector3 to)
    {
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
        lr.numCapVertices = 4;  
        lr.sortingLayerName = sortingLayerName;
    }

    private bool checkForShape()
    {
        throw new System.NotImplementedException();
    }
}