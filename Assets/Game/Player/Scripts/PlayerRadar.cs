using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Game.Core.Managers;
using Game.Core.Utils;
using Game.Platforms.Scripts;
using Spine.Unity;

namespace Game.Player.Scripts
{
    public class PlayerRadar
    {
        private Transform playerTransform;
        private PlayerStats playerStats;
        private PlayerLogger playerLogger;

        private List<GameObject> platformsInRange = new List<GameObject>();
        private List<GameObject> lightAreasInRange = new List<GameObject>();
        private List<MovingPlatform> _movingPlatformsInRange = new List<MovingPlatform>();

        private Coroutine movingPlatformCoroutine;
        private MonoBehaviour coroutineRunner;
        private readonly List<Transform> playerPlatforms;


        public PlayerRadar(Transform playerTransform, PlayerStats playerStats, PlayerLogger playerLogger,
            MonoBehaviour coroutineRunner, List<Transform> playerPlatforms)
        {
            this.playerTransform = playerTransform;
            this.playerStats = playerStats;
            this.playerLogger = playerLogger;
            this.playerPlatforms = playerPlatforms;

            coroutineRunner.StartCoroutine(MovingPlatformRoutine());
        }


        private IEnumerator MovingPlatformRoutine()
        {
            while (true)
            {
                yield return null;
                UpdatePlatformsAndLights();
            }
        }

        private void UpdatePlatformsAndLights()
        {
            

            // Find all platforms in range
            Collider2D[] hits = Physics2D.OverlapCircleAll(playerTransform.position, playerStats.radarRadius);

            // Build new sets for this frame
            var newPlatformsInRange = new HashSet<GameObject>();
            var newLightAreasInRange = new HashSet<GameObject>();
            var newMovingPlatformsInRange = new HashSet<MovingPlatform>();

            if (playerPlatforms.Count > 1)
            {
                var platGO = playerPlatforms[playerPlatforms.Count - 2].gameObject;
                UpdatePlatformsAndLightsProcessing(platGO, newLightAreasInRange, newMovingPlatformsInRange, newPlatformsInRange);
            }
            
            if (playerPlatforms.Count > 0)
            {
                var platGO = playerPlatforms[playerPlatforms.Count - 1].gameObject;
                UpdatePlatformsAndLightsProcessing(platGO, newLightAreasInRange, newMovingPlatformsInRange, newPlatformsInRange);
            }
            
            foreach (var hit in hits)
            {
                if (((1 << hit.gameObject.layer) & playerStats.platformLayer) != 0)
                {
                    var platGO = hit.gameObject;
                    var platGOTransform = EladsHelperFunctions.GetRootTransformPlatformHead(platGO.transform);
                    platGO = platGOTransform.gameObject;
                    UpdatePlatformsAndLightsProcessing(platGO, newLightAreasInRange, newMovingPlatformsInRange, newPlatformsInRange);
                }
            }

            // Disable light areas and moving platforms that are no longer in range
            foreach (var light in lightAreasInRange)
            {
                if (!newLightAreasInRange.Contains(light))
                    light.SetActive(false);
            }

            foreach (var movingPlatform in _movingPlatformsInRange)
            {
                if (!newMovingPlatformsInRange.Contains(movingPlatform))
                {
                    movingPlatform.SetSkinActive(false);
                }
            }

            // Enable new light areas and moving platforms
            foreach (var light in newLightAreasInRange)
            {
                if (!lightAreasInRange.Contains(light))
                    light.SetActive(true);
            }

            foreach (var movingPlatform in newMovingPlatformsInRange)
            {
                if (!_movingPlatformsInRange.Contains(movingPlatform))
                {
                    movingPlatform.SetSkinActive(true);
                }
            }

            // Update the lists for next frame
            platformsInRange = new List<GameObject>(newPlatformsInRange);
            lightAreasInRange = new List<GameObject>(newLightAreasInRange);
            _movingPlatformsInRange = new List<MovingPlatform>(newMovingPlatformsInRange);
            
        }

        private void UpdatePlatformsAndLightsProcessing(GameObject platGO,
            HashSet<GameObject> newLightAreasInRange,
            HashSet<MovingPlatform> newMovingPlatformsInRange, HashSet<GameObject> newPlatformsInRange)
        {
            newPlatformsInRange.Add(platGO);
            var platGoMoving = platGO.GetComponentInChildren<MovingPlatform>();
            if (platGoMoving != null && (platGoMoving.platformType == PlatformType.King || platGoMoving.platformType == PlatformType.Queen))
            {
                return;
            }
            foreach (Transform descendant in platGO.GetComponentsInChildren<Transform>(true))
            {
                if (descendant.CompareTag("LightAreaPlatform"))
                {
                    newLightAreasInRange.Add(descendant.gameObject);
                }

                var movingPlatform = descendant.GetComponent<MovingPlatform>();
                if (movingPlatform != null)
                {
                    newMovingPlatformsInRange.Add(movingPlatform);
                }
            }
            
            
            
        }

        public bool IsPlatformInRange(GameObject platform)
        {
            if (platform == null || playerTransform == null) return false;
            return platformsInRange.Contains(platform);
        }
    }
}