

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
        public static bool PointInPolygon(Vector2[] verts, Vector2 p)
        {
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

        
        

    }
}