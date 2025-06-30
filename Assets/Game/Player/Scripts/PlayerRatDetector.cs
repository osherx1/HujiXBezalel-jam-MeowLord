using System.Collections.Generic;
using System.Linq;
using Game.Core.Managers;
using Game.Core.Utils;
using Game.Enemies.Scripts;
using UnityEngine;

namespace Game.Player.Scripts
{
    public class PlayerRatDetector
    {
        public struct Polygon
        {
            public int highestIndexPlatform;
            public int highestSegmentIndex;
            public List<Transform> polygonPoints;
        }

        private List<Segment.TrailSegment> _segments;
        private List<Transform> _visited;
        private Transform _segmentsPointsFather;
        private List<GameObject> _intersectionPoints = new List<GameObject>();
        private readonly PlayerStats _playerStats;

        public PlayerRatDetector(List<Segment.TrailSegment> segments, List<Transform> visited, Transform segmentsPointsFather,PlayerStats playerStats)
        {
            _segments = segments;
            _visited = visited;
            _segmentsPointsFather = segmentsPointsFather;
            _playerStats = playerStats;
        }

        public List<Polygon> CheckForClosedPolygons()
        {
            var polygons = new List<Polygon>();
            ClearAllIntersections();
            // For each pair of non-adjacent segments, check for intersection
            for (int i = 0; i < _segments.Count - 2; i++)
            {
                var segA = _segments[i];
                Vector2 a1 = segA.FromT.TransformPoint(segA.FromLocalPos);
                Vector2 a2 = segA.ToT.TransformPoint(segA.ToLocalPos);
                for (int j = i + 2; j < _segments.Count; j++)
                {
                    // Skip adjacent segments
                    if (j == i + 1) continue;
                    var segB = _segments[j];
                    Vector2 b1 = segB.FromT.TransformPoint(segB.FromLocalPos);
                    Vector2 b2 = segB.ToT.TransformPoint(segB.ToLocalPos);
                    if (LineSegmentsIntersect(a1, a2, b1, b2, out Vector2 intersection))
                    {
                        // Create intersection point GameObject
                        var intersectionGO = new GameObject("IntersectionPoint");
                        intersectionGO.transform.position = intersection;
                        intersectionGO.transform.parent = _segmentsPointsFather;
                        _intersectionPoints.Add(intersectionGO);
                        // Build the polygon: from i+1 to j, plus the intersection points at start and end
                        var polygonPoints = new List<Transform>();
                        // in case of _visited change while function run
                        if (_visited.Count < i + 2)
                        {
                            return new List<Polygon>();
                        }
                        polygonPoints.Add(_visited[i + 1]); // Start after segA
                        for (int k = i + 2; k <= j; k++)
                        {
                            polygonPoints.Add(_visited[k]);
                        }
                        polygonPoints.Add(intersectionGO.transform); // Add intersection point at the end
                        polygonPoints.Insert(0, intersectionGO.transform); // Add intersection point at the start
                        // Find highest indices
                        int highestIndexPlatform = -1;
                        foreach (var t in polygonPoints)
                        {
                            int idx = _visited.IndexOf(t);
                            if (idx > highestIndexPlatform) highestIndexPlatform = idx;
                        }
                        int highestSegmentIndex = j;
                        polygons.Add(new Polygon
                        {
                            highestIndexPlatform = highestIndexPlatform,
                            highestSegmentIndex = highestSegmentIndex,
                            polygonPoints = polygonPoints
                        });
                    }
                }
            }
            return polygons;
        }

        public void ClearAllIntersections()
        {
            foreach (var go in _intersectionPoints)
            {
                if (go != null)
                    GameObject.Destroy(go);
            }
            _intersectionPoints.Clear();
        }

        // Helper: Check if two line segments intersect and get the intersection point
        private bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2, out Vector2 intersection)
        {
            intersection = Vector2.zero;
            float s1_x = p2.x - p1.x;
            float s1_y = p2.y - p1.y;
            float s2_x = q2.x - q1.x;
            float s2_y = q2.y - q1.y;

            float denom = (-s2_x * s1_y + s1_x * s2_y);
            if (Mathf.Abs(denom) < 1e-6f) return false; // Parallel or collinear

            float s = (-s1_y * (p1.x - q1.x) + s1_x * (p1.y - q1.y)) / denom;
            float t = ( s2_x * (p1.y - q1.y) - s2_y * (p1.x - q1.x)) / denom;

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
            {
                // Intersection detected
                intersection = new Vector2(p1.x + (t * s1_x), p1.y + (t * s1_y));
                return true;
            }
            return false;
        }

        public bool DestroyEnemiesInLoop(List<Transform> loopPlatforms, LayerMask enemyLayer)
        {
            var inPolygon = GetEnemiesInLoop(loopPlatforms, enemyLayer);
            if (inPolygon == null) return false;
            foreach (var c in inPolygon){
                ApplyDamageToRat(c);
            }
            GameEvents.ScoreCombinatorReady();
            return true;
        }

        public List<Collider2D> GetEnemiesInLoop(List<Transform> loopPlatforms, LayerMask enemyLayer)
        {
            var poly = GetPolygonPoints(loopPlatforms);
            var candidates = GetCandidateColliders(poly, enemyLayer);
            return CandidatesInPolygon(candidates, poly);
        }

        private List<Collider2D> CandidatesInPolygon(Collider2D[] candidates, Vector2[] poly)
        {
            return candidates.Where(c => c != null && EladsHelperFunctions.PointInPolygon(poly, c.transform.position,_playerStats.ForgivenceToPlayer)).ToList();
        }

        private Collider2D[] GetCandidateColliders(Vector2[] poly, LayerMask enemyLayer)
        {
            float minX = poly.Min(v => v.x), maxX = poly.Max(v => v.x);
            float minY = poly.Min(v => v.y), maxY = poly.Max(v => v.y);
            Vector2 min = new Vector2(minX - _playerStats.ForgivenceToPlayer, minY - _playerStats.ForgivenceToPlayer);
            Vector2 max = new Vector2(maxX + _playerStats.ForgivenceToPlayer, maxY + _playerStats.ForgivenceToPlayer);
            return Physics2D.OverlapAreaAll(min, max, enemyLayer);
        }

    

        public void ApplyDamageToRat(Collider2D collider)
        {
            RatHealth ratHealth = collider.GetComponent<RatHealth>();
            if (ratHealth != null)
            {
                ratHealth.TakeDamage(1); // Or however much damage the cat deals
            }
        }

        private Vector2[] GetPolygonPoints(List<Transform> loopPlatforms)
            {
                return loopPlatforms
                    .Select(t => (Vector2)t.position)
                    .ToArray();
            }


        public bool HasKingOrQueenInLoopPolygon(List<Transform> loopPlatforms, LayerMask platformLayer)
        {
            var poly = GetPolygonPoints(loopPlatforms);
            var candidates = GetCandidateColliders(poly, platformLayer);
            foreach (var c in candidates)
            {
                if (c == null) continue;
                var root = EladsHelperFunctions.GetRootTransformPlatformHead(c.transform);
                if (root == null) continue;
                var movingPlatform = root.GetComponent<Game.Platforms.Scripts.MovingPlatform>();
                if (movingPlatform != null &&
                    (movingPlatform.platformType == Game.Platforms.Scripts.PlatformType.King ||
                     movingPlatform.platformType == Game.Platforms.Scripts.PlatformType.Queen))
                {
                    if (EladsHelperFunctions.PointInPolygon(poly, root.position))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}