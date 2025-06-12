using System;
using UnityEngine;

namespace Game.Core.Utils
{
    public static class EladsHelperFunctions
    {
        /// <summary>
        /// Returns true if p is inside the polygon defined by verts (must be in order).
        /// Uses the ray-crossing algorithm.
        /// </summary>
        public static bool PointInPolygon(Vector2[] verts, Vector2 p, float tolerance = 0.1f)
        {
            // First, check if the point is close to any edge
            for (int i = 0, j = verts.Length - 1; i < verts.Length; j = i++)
            {
                Vector2 vi = verts[i], vj = verts[j];
                if (DistancePointToSegment(p, vi, vj) <= tolerance)
                    return true;
            }

            // Standard ray-casting algorithm
            bool inside = false;
            for (int i = 0, j = verts.Length - 1; i < verts.Length; j = i++)
            {
                Vector2 vi = verts[i], vj = verts[j];
                bool intersect = ((vi.y > p.y) != (vj.y > p.y)) &&
                                 (p.x < (vj.x - vi.x) * (p.y - vi.y) / (vj.y - vi.y) + vi.x);
                if (intersect) inside = !inside;
            }
            return inside;
        }

        // Helper: Distance from point p to segment ab
        public static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 ap = p - a;
            float t = Vector2.Dot(ap, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            Vector2 closest = a + t * ab;
            return Vector2.Distance(p, closest);
        }
        
        
        public static bool IsWithinBoundsXY(Bounds b, Vector3 pos)
        {
            return pos.x >= b.min.x && pos.x <= b.max.x &&
                   pos.y >= b.min.y && pos.y <= b.max.y;
        }

        public static Rect GetCenteredRect(float widthPercent, float heightPercent)
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float w = screenWidth * widthPercent;
            float h = screenHeight * heightPercent;
            return new Rect(
                (screenWidth - w) / 2f,
                (screenHeight - h) / 2f,
                w,
                h
            );
        }

        public static Vector3 ClampPositionToBounds(Bounds bounds, Vector3 position)
        {
            var min = bounds.min;
            var max = bounds.max;
            return new Vector3(
                Mathf.Clamp(position.x, min.x, max.x),
                Mathf.Clamp(position.y, min.y, max.y),
                position.z
            );
        }

        public static Transform GetRootTransformPlatformHead(Transform t)
        {
            if (t == null) return null;
            while (t.parent != null && !t.CompareTag($"PlatformHead"))
                t = t.parent;
            return t;
        }

        
        

    }
}