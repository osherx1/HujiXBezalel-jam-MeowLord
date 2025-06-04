using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Core.Managers;

namespace Game.Player.Scripts
{
    public class PlayerRadar
    {
        private Transform playerTransform;
        private PlayerStats playerStats;
        private PlayerLogger playerLogger;

        private List<GameObject> platformsInRange = new List<GameObject>();
        private List<GameObject> lightAreasInRange = new List<GameObject>();

        private Coroutine movingPlatformCoroutine;
        private MonoBehaviour coroutineRunner;

        public PlayerRadar(Transform playerTransform, PlayerStats playerStats, PlayerLogger playerLogger, MonoBehaviour coroutineRunner)
        {
            this.playerTransform = playerTransform;
            this.playerStats = playerStats;
            this.playerLogger = playerLogger;
            
            coroutineRunner.StartCoroutine(MovingPlatformRoutine());
        }

        
        private IEnumerator MovingPlatformRoutine()
        {
            while (true)
            {
                yield return null; // Wait one frame
                UpdatePlatformsAndLights();
            }
        }

        private void UpdatePlatformsAndLights()
        {
            playerLogger?.Log("PlayerRadar: Updating platforms and lights in range.");
            // Only disable previous light areas
            foreach (var light in lightAreasInRange)
            {
                light.SetActive(false);
            }
            platformsInRange.Clear();
            lightAreasInRange.Clear();

            // Find all platforms in range
            Collider2D[] hits = Physics2D.OverlapCircleAll(playerTransform.position, playerStats.radarRadius);
            foreach (var hit in hits)
            {
                // Check if the hit is in the platform layer from playerStats
                if (((1 << hit.gameObject.layer) & playerStats.platformLayer) != 0)
                {
                    var platGO = hit.gameObject;
                    platformsInRange.Add(platGO);

                    // Find any descendant with tag "LightAreaPlatform"
                    foreach (Transform descendant in platGO.GetComponentsInChildren<Transform>(true))
                    {
                        if (descendant.CompareTag("LightAreaPlatform"))
                        {
                            descendant.gameObject.SetActive(true);
                            lightAreasInRange.Add(descendant.gameObject);
                        }
                    }
                }
            }
            playerLogger?.Log($"PlayerRadar: Enabled {lightAreasInRange.Count} light areas.");
        }

        public bool IsPlatformInRange(GameObject platform)
        {
            if (platform == null || playerTransform == null) return false;
            return platformsInRange.Contains(platform);
        }
    }
}