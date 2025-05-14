

using UnityEngine;

namespace Game.Core.Utils
{
    public class EladsHelperFunctions
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
    }
}