using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Platforms.Scripts;

namespace Game.Platforms.Scripts
{
    public class MovingPlatformPool : MonoBehaviour
    {
        [System.Serializable]
        public class PlatformPrefabData
        {
            public PlatformType type;
            public MovingPlatform prefab;
        }

        [System.Serializable]
        public class RouteGroup
        {
            public List<PlatformType> allowedTypes; // List of allowed platform types for these routes
            public List<GameObject> routes;
        }


        [Header("All platform prefabs")] public List<PlatformPrefabData> platformPrefabs;

        [Header("Pool size per type")] public int initialPoolSize = 5;

        private Dictionary<PlatformType, Queue<MovingPlatform>> pools =
            new Dictionary<PlatformType, Queue<MovingPlatform>>();

        [Header("Spawn position (all platforms start here)")]
        public Transform spawnPoint;

        [Header("Available routes for each type")]
        public List<RouteGroup> routesSetup;

        [Header("Limit groups (can overlap and be of any size)")]
        public List<LimitGroup> limitGroups = new();

        private int[] groupActiveCounts;
        private Dictionary<PlatformType, int> activeCountByType = new();
        private bool allowTwoRoyals = false;
        private int activeRoyals = 0;
        private PlatformType[] royals = { PlatformType.King, PlatformType.Queen };

        // Internal lookup for fast access
        private Dictionary<PlatformType, List<GameObject>> routesByType =
            new Dictionary<PlatformType, List<GameObject>>();

        // Track which routes are currently in use
        private Dictionary<GameObject, bool> activeRoutes = new Dictionary<GameObject, bool>();

        private Dictionary<GameObject, List<PlatformType>> allowedTypesPerRoute =
            new Dictionary<GameObject, List<PlatformType>>();

        void Awake()
        {
            // Existing pool init
            foreach (var data in platformPrefabs)
            {
                var queue = new Queue<MovingPlatform>();
                for (int i = 0; i < initialPoolSize; i++)
                {
                    var obj = Instantiate(data.prefab, spawnPoint.position, Quaternion.identity, this.transform);
                    obj.gameObject.SetActive(false);
                    obj.platformType = data.type;
                    queue.Enqueue(obj);
                }

                pools[data.type] = queue;

                // Init active counters for each type
                activeCountByType[data.type] = 0;
            }

            groupActiveCounts = new int[limitGroups.Count];

            // routes/activeRoutes logic as before...
            foreach (var group in routesSetup)
            {
                foreach (var route in group.routes)
                {
                    if (!allowedTypesPerRoute.ContainsKey(route))
                        allowedTypesPerRoute[route] = new List<PlatformType>();
                    foreach (var type in group.allowedTypes)
                        if (!allowedTypesPerRoute[route].Contains(type))
                            allowedTypesPerRoute[route].Add(type);
                    if (!activeRoutes.ContainsKey(route))
                        activeRoutes[route] = false;
                }
            }

            // Subscribe to score update event
            Game.Core.Managers.GameEvents.OnUpdateScore += OnScoreUpdated;
        }


        public void SpawnPlatform(PlatformType type)
        {
            if (activeCountByType.ContainsKey(type) && activeCountByType[type] >= 3)
            {
                return;
            }

            bool isRoyal = royals.Contains(type);
            if (isRoyal)
            {
                if (!allowTwoRoyals && activeRoyals >= 1)
                {
                    Debug.Log("Only one royal allowed before 10,000 points");
                    return;
                }

                if (allowTwoRoyals && activeRoyals >= 2)
                {
                    Debug.Log("Only two royals allowed after 10,000 points");
                    return;
                }
            }

            for (int i = 0; i < limitGroups.Count; i++)
            {
                if (limitGroups[i].types.Contains(type) && groupActiveCounts[i] >= limitGroups[i].maxActive)
                {
                    return;
                }
            }

            if (!pools.ContainsKey(type))
            {
                Debug.LogError("No pool for type: " + type);
                return;
            }

            // Build a list of all available (not active) routes for this platform type
            List<GameObject> candidateRoutes = new List<GameObject>();
            foreach (var route in allowedTypesPerRoute.Keys)
            {
                if (allowedTypesPerRoute[route].Contains(type) && !activeRoutes[route])
                    candidateRoutes.Add(route);
            }

            if (candidateRoutes.Count == 0)
            {
                Debug.Log("All routes for type " + type + " are currently active or no route allows this type.");
                return; // No available route
            }

            // Randomly pick one of the available candidate routes
            GameObject chosenRoute = candidateRoutes[Random.Range(0, candidateRoutes.Count)];

            MovingPlatform platform;
            if (pools[type].Count > 0)
            {
                platform = pools[type].Dequeue();
            }
            else
            {
                var prefab = platformPrefabs.Find(p => p.type == type).prefab;
                platform = Instantiate(prefab, spawnPoint.position, Quaternion.identity, this.transform);
                platform.platformType = type;
            }

            // Mark the chosen route as active
            activeRoutes[chosenRoute] = true;

            platform.gameObject.SetActive(true);
            platform.transform.position = spawnPoint.position;

            activeCountByType[type]++;
            if (isRoyal)
                activeRoyals++;

            for (int i = 0; i < limitGroups.Count; i++)
            {
                if (limitGroups[i].types.Contains(type))
                    groupActiveCounts[i]++;
            }

            // Pass the chosen route and a callback that knows which route to release
            platform.Init(chosenRoute, platform.moveSpeed, (p) =>
            {
                ReturnToPool(p, chosenRoute);
                platform.PlatformReturn();
            });
        }


        public void SpawnPlatform(PlatformType type, GameObject route)
        {
            for (int i = 0; i < limitGroups.Count; i++)
            {
                if (limitGroups[i].types.Contains(type) && groupActiveCounts[i] >= limitGroups[i].maxActive)
                {
                    Debug.Log(
                        $"Spawn blocked: limit group {i} (types: {string.Join(",", limitGroups[i].types)}) reached {groupActiveCounts[i]}/{limitGroups[i].maxActive}");
                    return;
                }
            }

            if (!pools.ContainsKey(type))
            {
                Debug.LogError("No pool for type: " + type);
                return;
            }

            if (route == null)
            {
                Debug.LogError("Route is null for type: " + type);
                return;
            }

            if (activeRoutes.ContainsKey(route) && activeRoutes[route])
            {
                Debug.Log($"Route is already active for type: {type}");
                return;
            }

            MovingPlatform platform;
            if (pools[type].Count > 0)
            {
                platform = pools[type].Dequeue();
            }
            else
            {
                var prefab = platformPrefabs.Find(p => p.type == type).prefab;
                platform = Instantiate(prefab, spawnPoint.position, Quaternion.identity, this.transform);
                platform.platformType = type;
            }


            platform.gameObject.SetActive(true);
            platform.transform.position = spawnPoint.position;

            for (int i = 0; i < limitGroups.Count; i++)
            {
                if (limitGroups[i].types.Contains(type))
                    groupActiveCounts[i]++;
            }

            // Pass the chosen route and a callback that knows which route to release
            platform.Init(route, platform.moveSpeed, (p) =>
            {
                ReturnToPool(p);
                platform.PlatformReturn();
            });
        }

        private void ReturnToPool(MovingPlatform platform, GameObject route)
        {
            platform.transform.position = spawnPoint.position;
            platform.gameObject.SetActive(false);
            platform.EnableAllPolygonColliders();
            pools[platform.platformType].Enqueue(platform);
            activeRoutes[route] = false;

            if (activeCountByType.ContainsKey(platform.platformType))
                activeCountByType[platform.platformType]--;

            if (royals.Contains(platform.platformType) && activeRoyals > 0)
                activeRoyals--;

            var collider = platform.GetComponent<Collider2D>();
            if (collider != null)
                collider.enabled = true;

            // Decrement all group counters that include this type
            for (int i = 0; i < limitGroups.Count; i++)
            {
                if (limitGroups[i].types.Contains(platform.platformType))
                    groupActiveCounts[i]--;
            }
        }


        public void ReturnToPool(MovingPlatform platform)
        {
            platform.transform.position = spawnPoint.position;
            platform.gameObject.SetActive(false);
            platform.EnableAllPolygonColliders();
            pools[platform.platformType].Enqueue(platform);

            if (activeCountByType.ContainsKey(platform.platformType))
                activeCountByType[platform.platformType]--;

            if (royals.Contains(platform.platformType) && activeRoyals > 0)
                activeRoyals--;

            var collider = platform.GetComponent<Collider2D>();
            if (collider != null)
                collider.enabled = true;

            for (int i = 0; i < limitGroups.Count; i++)
            {
                if (limitGroups[i].types.Contains(platform.platformType))
                    groupActiveCounts[i]--;
            }
        }


        void OnDisable()
        {
            Game.Core.Managers.GameEvents.OnUpdateScore -= OnScoreUpdated;
        }

        private void OnScoreUpdated(int score)
        {
            allowTwoRoyals = score >= 5000;
        }
    }
}