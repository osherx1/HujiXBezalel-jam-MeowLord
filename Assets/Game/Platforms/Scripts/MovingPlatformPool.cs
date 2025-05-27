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

        [Header("All platform prefabs")]
        public List<PlatformPrefabData> platformPrefabs;

        [Header("Pool size per type")]
        public int initialPoolSize = 5;

        private Dictionary<PlatformType, Queue<MovingPlatform>> pools = new Dictionary<PlatformType, Queue<MovingPlatform>>();

        [Header("Spawn position (all platforms start here)")]
        public Transform spawnPoint;

        [Header("Available routes by type")]
        public List<RouteSelector> routesByType;

        [System.Serializable]
        public class RouteSelector
        {
            public PlatformType type;
            public List<GameObject> possibleRoutes; // List of parent GameObjects, each with children as waypoints
        }

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
                // If empty, instantiate a new one
                var prefab = platformPrefabs.Find(p => p.type == type).prefab;
                platform = Instantiate(prefab, spawnPoint.position, Quaternion.identity, this.transform);
                platform.platformType = type;
            }

            // Select random route for this type
            var routeList = routesByType.Find(r => r.type == type)?.possibleRoutes;
            if (routeList == null || routeList.Count == 0)
            {
                Debug.LogError("No routes set for type: " + type);
                return;
            }
            var chosenRoute = routeList[Random.Range(0, routeList.Count)];

            // Initialize and activate
            platform.transform.position = spawnPoint.position;
            platform.Init(chosenRoute, platform.moveSpeed, ReturnToPool);

            // (Optional) Set color, animation, etc. according to type here
        }

        private void ReturnToPool(MovingPlatform platform)
        {
            platform.transform.position = spawnPoint.position;
            pools[platform.platformType].Enqueue(platform);
        }
    }
}
