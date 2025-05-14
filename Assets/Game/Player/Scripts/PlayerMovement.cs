using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core.Input;

namespace Game.Player.Scripts
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Click Settings")]
        [SerializeField] private LayerMask clickableLayer;   

        [Header("Trail Settings")]
        [SerializeField] private Material lineMaterial;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private string sortingLayerName;
        
        private class TrailSegment
        {
            public LineRenderer lr;
            public Transform fromT, toT;
            public Vector3 fromLocalPos, toLocalPos;
        }

        private List<TrailSegment> _segments = new List<TrailSegment>();
        private GameObject   _lastPlatform;
        private Camera       _mainCam;
        private InputAction  _clickAction;

        void Awake()
        {
            _mainCam     = Camera.main;
            _clickAction = InputSystemSingleton.Instance.InputSystem.PlayerControls.Click;
        }

        void OnEnable()  => _clickAction.performed += OnClick;
        void OnDisable() => _clickAction.performed -= OnClick;

        private void OnClick(InputAction.CallbackContext ctx)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = _mainCam.ScreenToWorldPoint(screenPos);

            var hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, clickableLayer);
            if (hit.collider == null) return;

            var newPlatform = hit.collider.gameObject;

            if (_lastPlatform != null && newPlatform != _lastPlatform)
                CreateSegment(_lastPlatform, newPlatform);

            _lastPlatform = newPlatform;
            transform.position = newPlatform.transform.position;
        }

        private void CreateSegment(GameObject fromGO, GameObject toGO)
        {
            Vector3 fromWorld = fromGO.transform.position;
            Vector3 toWorld   = toGO.transform.position;

            // Compute local offsets
            Vector3 fromLocal = fromGO.transform.InverseTransformPoint(fromWorld);
            Vector3 toLocal   = toGO.transform.InverseTransformPoint(toWorld);

            // Build line
            var lineObj = new GameObject("ClickTrail");
            var lr = lineObj.AddComponent<LineRenderer>();
            lr.useWorldSpace   = true;                 // we'll drive positions manually
            lr.positionCount   = 2;
            lr.startWidth      = lineWidth;
            lr.endWidth        = lineWidth;
            lr.numCapVertices  = 4;
            lr.material        = lineMaterial;
            lr.sortingLayerName= sortingLayerName;

            // Initial “dumb” set so we see it immediately:
            lr.SetPosition(0, fromWorld);
            lr.SetPosition(1, toWorld);

            // Store it
            _segments.Add(new TrailSegment {
                lr           = lr,
                fromT        = fromGO.transform,
                toT          = toGO.transform,
                fromLocalPos = fromLocal,
                toLocalPos   = toLocal
            });
        }

        void LateUpdate()
        {
            // Recompute all segment endpoints
            foreach (var seg in _segments)
            {
                Vector3 p0 = seg.fromT.TransformPoint(seg.fromLocalPos);
                Vector3 p1 = seg.toT  .TransformPoint(seg.toLocalPos);
                seg.lr.SetPosition(0, p0);
                seg.lr.SetPosition(1, p1);
            }
        }
    }
}
