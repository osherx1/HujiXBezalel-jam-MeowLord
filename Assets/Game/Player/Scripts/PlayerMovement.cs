using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core.Input;
using Game.Core.Managers;
using Game.Core.Utils;


namespace Game.Player.Scripts
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Click Settings")] [SerializeField]
        private LayerMask clickableLayer;

        [SerializeField] private LayerMask enemyLayer;


        [Header("Trail Settings")] [SerializeField]
        private Material lineMaterial;

        [SerializeField] private float delayForSegments = 0.2f;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private string sortingLayerName;

        private class TrailSegment
        {
            public LineRenderer Lr;
            public Transform FromT, ToT;
            public Vector3 FromLocalPos, ToLocalPos;
        }

        private List<TrailSegment> _segments = new List<TrailSegment>();
        private List<Transform> _visited = new List<Transform>();
        private Transform _lastPlat;
        private Camera _mainCam;
        private InputAction _clickAction;

        void Awake()
        {
            _mainCam = Camera.main;
            _clickAction = InputSystemSingleton.Instance.InputSystem.PlayerControls.Click;
        }

        void Start()
        {
            // 1) find every Collider2D in the scene whose GameObject lives on your clickableLayer
            var nearest = FindNearestPlatformer();

            // 2) if we found one, snap onto it, set _lastPlat and _visited
            SetPlayerToNearestPlatform(nearest);
        }

        private void SetPlayerToNearestPlatform(Transform nearest)
        {
            if (nearest != null)
            {
                _lastPlat = nearest;
                _visited.Add(nearest);
                transform.position = nearest.position;
            }
        }

        private Transform FindNearestPlatformer()
        {
            float searchRadius = 10f;
            Collider2D[] all = Physics2D.OverlapCircleAll(
                transform.position,
                searchRadius,
                clickableLayer
            );
            float minDist = float.MaxValue;
            Transform nearest = null;

            foreach (var c in all)
            {
                float d = Vector2.Distance(transform.position, c.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = c.transform;
                }
            }

            return nearest;
        }

        void OnEnable() => _clickAction.performed += OnClick;
        void OnDisable() => _clickAction.performed -= OnClick;

        private void OnClick(InputAction.CallbackContext ctx)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = _mainCam.ScreenToWorldPoint(screenPos);
            var hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, clickableLayer);
            if (hit.collider == null) return;

            var newPlat = hit.collider.transform;

            // 1. first click: just initialize
            if (_lastPlat == null)
            {
                _lastPlat = newPlat;
                _visited.Add(newPlat);
            }
            else if (newPlat != _lastPlat) // a real move
            {
                // 2. cycle detection:
                CreateSegment(_lastPlat.gameObject, newPlat.gameObject);
                if (_visited.Contains(newPlat))
                {
                    int idx = _visited.IndexOf(newPlat);

                    // 1) snapshot exactly the loop of platforms
                    List<Transform> loopPlatforms = _visited.GetRange(
                        idx,
                        _visited.Count - idx // <-- no +1 here
                    );

                    // 2) immediately prune your history
                    _visited.RemoveRange(idx+1, _visited.Count - idx -1);

                    // 3) schedule the LineRenderer cleanup
                    DOVirtual.DelayedCall(delayForSegments, () =>
                    {
                        for (int i = _segments.Count - 1; i >= idx; i--)
                        {
                            Destroy(_segments[i].Lr.gameObject);
                            _segments.RemoveAt(i);
                        }
                    });

                    // 4) destroy enemies inside that polygon
                    DestroyEnemiesInLoop(loopPlatforms);
                }

                else
                {
                    // 3. no loop → create a brand‐new segment
                    _visited.Add(newPlat);
                }
                GameEvents.PlayerMoved();
                _lastPlat = newPlat;
                transform.position = newPlat.position;
            }
        }

        private void CreateSegment(GameObject fromGo, GameObject toGo)
        {
            // compute local offsets
            var fromT = fromGo.transform;
            var toT = toGo.transform;
            Vector3 fromLocal = fromT.InverseTransformPoint(fromT.position);
            Vector3 toLocal = toT.InverseTransformPoint(toT.position);

            // build the line in world‐space (we’ll reposition each frame)
            var go = new GameObject("ClickTrail");
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.numCapVertices = 4;
            lr.material = lineMaterial;
            lr.sortingLayerName = sortingLayerName;
            lr.SetPosition(0, fromT.position);
            lr.SetPosition(1, toT.position);

            _segments.Add(new TrailSegment
            {
                Lr = lr,
                FromT = fromT,
                ToT = toT,
                FromLocalPos = fromLocal,
                ToLocalPos = toLocal
            });
        }


        private void DestroyEnemiesInLoop(List<Transform> loopPlatforms)
        {
            // 1) gather the ordered vertices of the loop
            Vector2[] poly = loopPlatforms
                .Select(t => (Vector2)t.position)
                .ToArray();

            // 2) compute bounding box so OverlapAreaAll is efficient
            float minX = poly.Min(v => v.x), maxX = poly.Max(v => v.x);
            float minY = poly.Min(v => v.y), maxY = poly.Max(v => v.y);
            Vector2 min = new Vector2(minX, minY);
            Vector2 max = new Vector2(maxX, maxY);

            // 3) get all enemy colliders in that AABB
            Collider2D[] candidates =
                Physics2D.OverlapAreaAll(min, max, enemyLayer);

            // 4) for each, test the precise polygon
            foreach (var c in candidates)
            {
                Vector2 pt = c.transform.position;
                if (EladsHelperFunctions.PointInPolygon(poly, pt))
                    Destroy(c.gameObject);
            }
        }

        void LateUpdate()
        {
            // each frame, re‐anchor all segment endpoints
            foreach (var seg in _segments)
            {
                seg.Lr.SetPosition(0, seg.FromT.TransformPoint(seg.FromLocalPos));
                seg.Lr.SetPosition(1, seg.ToT.TransformPoint(seg.ToLocalPos));
                transform.position = seg.ToT.position;
            }
        }
    }
}