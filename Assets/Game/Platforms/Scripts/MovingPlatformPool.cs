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
            public List<PlatformType> allowedTypes; // List of allowed platform types for these routes
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
        private Dictionary<GameObject, List<PlatformType>> allowedTypesPerRoute = new Dictionary<GameObject, List<PlatformType>>();

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
                foreach (var route in group.routes)
                {
                    if (!allowedTypesPerRoute.ContainsKey(route))
                        allowedTypesPerRoute[route] = new List<PlatformType>();

                    foreach (var type in group.allowedTypes)
                    {
                        if (!allowedTypesPerRoute[route].Contains(type))
                            allowedTypesPerRoute[route].Add(type);
                    }
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

            GameObject chosenRoute = null;
            foreach (var route in allowedTypesPerRoute.Keys)
            {
                if (allowedTypesPerRoute[route].Contains(type) && !activeRoutes[route])
                {
                    chosenRoute = route;
                    break;
                }
            }

            if (chosenRoute == null)
            {
                Debug.Log("All routes for type " + type + " are currently active or no route allows this type.");
                return; // No available route
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
