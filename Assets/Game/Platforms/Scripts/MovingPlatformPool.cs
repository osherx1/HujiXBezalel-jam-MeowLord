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

            // Build routes lookup
            foreach (var group in routesSetup)
            {
                routesByType[group.type] = group.routes;
            }
        }

        public void SpawnPlatform(PlatformType type)
        {
            if (!pools.ContainsKey(type))
            {
                Debug.LogError("No pool for type: " + type);
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

            // Select random route for this type from the pool's setup
            if (!routesByType.ContainsKey(type) || routesByType[type] == null || routesByType[type].Count == 0)
            {
                Debug.LogError("No routes set for type: " + type);
                return;
            }
            var chosenRoute = routesByType[type][Random.Range(0, routesByType[type].Count)];

            platform.gameObject.SetActive(true);
            platform.transform.position = spawnPoint.position;
            platform.Init(chosenRoute, platform.moveSpeed, ReturnToPool);
        }

        private void ReturnToPool(MovingPlatform platform)
        {
            platform.transform.position = spawnPoint.position;
            pools[platform.platformType].Enqueue(platform);
        }
    }
}

