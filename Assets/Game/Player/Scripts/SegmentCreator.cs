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

        public static TrailSegment CreateSegment(GameObject linePrefab, Transform parent, GameObject fromGo, GameObject toGo)
        {
            // Instantiate the prefab and parent it
            var lineObject = Object.Instantiate(linePrefab, parent);
            lineObject.name = "ClickTrail";

            // Optionally reset local transform
            lineObject.transform.localPosition = Vector3.zero;
            lineObject.transform.localRotation = Quaternion.identity;
            lineObject.transform.localScale = Vector3.one;

            // Ensure LineRenderer
            var lr = lineObject.GetComponent<LineRenderer>();
            if (lr == null)
                lr = lineObject.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;

            // Set positions
            var fromT = fromGo.transform;
            var toT = toGo.transform;
            Vector3 fromLocal = fromT.InverseTransformPoint(fromT.position);
            Vector3 toLocal = toT.InverseTransformPoint(toT.position);

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