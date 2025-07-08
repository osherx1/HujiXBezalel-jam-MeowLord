using System;
using Game.Platforms.Scripts;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Player.Scripts
{
    public class Segment: MonoBehaviour
    {
        public class TrailSegment
        {
            public LineRenderer Lr;
            public Transform FromT, ToT;
            public Vector3 FromLocalPos, ToLocalPos;
            public BoxCollider2D BoxCollider;
        }
        
        public static event Action OnSegmentKingQueenCollide;
        
        private TrailSegment trailData;
        
        private float _nudgeTimer;
        
        
        [InspectorButton]
        private void UpdateBoxCollider()
        {
            if (trailData?.FromT == null || trailData?.ToT == null) 
            {
                Debug.LogWarning("Cannot update box collider: FromT or ToT is null.");
                return;
            }
            
            Vector2 fromPos = trailData.FromT.position;
            Vector2 toPos = trailData.ToT.position;
            Vector2 direction = (toPos - fromPos).normalized;
            float distance = Vector2.Distance(fromPos, toPos);
            
            trailData.BoxCollider.size = new Vector2(distance, trailData.Lr.endWidth); 
            trailData.BoxCollider.transform.position = (fromPos + toPos) * 0.5f;
            trailData.BoxCollider.offset = Vector2.zero;
            
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            trailData.BoxCollider.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

       
        public void LateUpdate()
        {
            UpdatePosition();
            // UpdateBoxCollider();
        }
        
        [InspectorButton]
        private void UpdatePosition()
        {
            trailData.Lr.SetPosition(0, trailData.FromT.TransformPoint(trailData.FromLocalPos));
            trailData.Lr.SetPosition(1, trailData.ToT.TransformPoint(trailData.ToLocalPos));
        }


        public static TrailSegment CreateSegment(GameObject linePrefab, Transform parent, GameObject fromGo, GameObject toGo)
        {
            // Instantiate the prefab and parent it
            var lineObject = Instantiate(linePrefab);
            lineObject.name = "ClickTrail";

            // Optionally reset local transform
            lineObject.transform.localPosition = Vector3.zero;
            lineObject.transform.localRotation = Quaternion.identity;
            lineObject.transform.localScale = Vector3.one;

            // Ensure LineRenderer
            var lr = lineObject.GetComponent<LineRenderer>();
            if (lr == null)
                lr = lineObject.AddComponent<LineRenderer>();
            var box = lineObject.GetComponentInChildren<BoxCollider2D>();
            if (box == null)
                Debug.LogError(new Exception("No BoxCollider2D component attached to a child."));
            box.offset = Vector2.zero;
            box.size = Vector2.zero;
            lr.useWorldSpace = true;
            lr.positionCount = 2;

            // Set positions
            var fromT = fromGo.transform;
            var toT = toGo.transform;
            Vector3 fromLocal = fromT.InverseTransformPoint(fromT.position);
            Vector3 toLocal = toT.InverseTransformPoint(toT.position);

            lr.SetPosition(0, fromT.position);
            lr.SetPosition(1, toT.position);
            var seg = lineObject.GetComponent<Segment>();
            seg.trailData = new TrailSegment
            {
                Lr = lr,
                FromT = fromT,
                ToT = toT,
                FromLocalPos = fromLocal,
                ToLocalPos = toLocal,
                BoxCollider = box,
            };
            
            return seg.trailData;
        }
        
        public void OnChildTriggerEnter(Collider2D other)
        {
            OnSegmentKingQueenCollide?.Invoke();
        }
    }
}