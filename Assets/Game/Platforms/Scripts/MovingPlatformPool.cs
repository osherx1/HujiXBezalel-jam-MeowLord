using System.Collections.Generic;
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
            public PlatformType type;
            public List<GameObject> routes;
        }

        [Header("All platform prefabs")]
        public List<PlatformPrefabData> platformPrefabs;

        [Header("Pool size per type")]
        public int initialPoolSize = 5;

        private Dictionary<PlatformType, Queue<MovingPlatform>> pools = new Dictionary<PlatformType, Queue<MovingPlatform>>();

        [Header("Spawn position (all platforms start here)")]
        public Transform spawnPoint;

        [Header("Available routes for each type")]
        public List<RouteGroup> routesSetup;

        // Internal lookup for fast access
        private Dictionary<PlatformType, List<GameObject>> routesByType = new Dictionary<PlatformType, List<GameObject>>();

        // Track which routes are currently in use
        private Dictionary<GameObject, bool> activeRoutes = new Dictionary<GameObject, bool>();

        void Awake()
        {
            // Initialize pools
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
            }

            // Build routes lookup and activeRoutes flags
            foreach (var group in routesSetup)
            {
                routesByType[group.type] = group.routes;
                foreach (var route in group.routes)
                {
                    if (!activeRoutes.ContainsKey(route))
                        activeRoutes[route] = false;
                }
            }
        }

        public void SpawnPlatform(PlatformType type)
        {
            if (!pools.ContainsKey(type))
            {
                Debug.LogError("No pool for type: " + type);
                return;
            }

            // Only choose a route that is currently not in use
            if (!routesByType.ContainsKey(type) || routesByType[type] == null || routesByType[type].Count == 0)
            {
                Debug.LogError("No routes set for type: " + type);
                return;
            }

            GameObject chosenRoute = null;
            foreach (var route in routesByType[type])
            {
                if (!activeRoutes[route])
                {
                    chosenRoute = route;
                    break;
                }
            }

            if (chosenRoute == null)
            {
                Debug.Log("All routes for " + type + " are currently active. No spawn.");
                return; // Don't spawn if all routes are busy
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

            // Mark the chosen route as active
            activeRoutes[chosenRoute] = true;

            platform.gameObject.SetActive(true);
            platform.transform.position = spawnPoint.position;

            // Pass the chosen route and a callback that knows which route to release
            platform.Init(chosenRoute, platform.moveSpeed, (p) =>
            {
                ReturnToPool(p, chosenRoute);
                platform.PlatformReturn();
            });
    }

        private void ReturnToPool(MovingPlatform platform, GameObject route)
        {
            platform.transform.position = spawnPoint.position;
            platform.gameObject.SetActive(false); 
            pools[platform.platformType].Enqueue(platform);
            activeRoutes[route] = false;
        }
    }
}
