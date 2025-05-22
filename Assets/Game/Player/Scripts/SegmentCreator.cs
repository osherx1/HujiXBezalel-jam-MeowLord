using UnityEngine;

namespace Game.Player.Scripts
{
    public static class SegmentCreator
    {
        public class TrailSegment
        {
            public LineRenderer Lr;
            public Transform FromT, ToT;
            public Vector3 FromLocalPos, ToLocalPos;
        }

        public static TrailSegment CreateSegment(GameObject fromGo, GameObject toGo, float lineWidth, Material lineMaterial, string sortingLayerName)
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
            return new TrailSegment
            {
                Lr = lr,
                FromT = fromT,
                ToT = toT,
                FromLocalPos = fromLocal,
                ToLocalPos = toLocal
            };
        }
        
    }
}