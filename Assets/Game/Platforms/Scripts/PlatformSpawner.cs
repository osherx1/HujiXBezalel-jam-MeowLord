using UnityEngine;
using Game.Platforms.Scripts;

public class PlatformSpawner : MonoBehaviour
{
    [Header("Reference to the Pool")]
    public MovingPlatformPool pool;

    [Header("Platform types to spawn")]
    public PlatformType[] spawnTypes;  // Array of possible platform types

    [Header("Which one to spawn (index in array)")]
    [Tooltip("Select index of platform to spawn (0 = first in list)")]
    public int selectedSpawnIndex = 0; // Index of platform to spawn

    [Header("Spawn every N seconds")]
    public float spawnInterval = 5f;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            if (pool != null && spawnTypes != null && spawnTypes.Length > 0)
            {
                // Clamp the index to make sure it is within array bounds
                int clampedIndex = Mathf.Clamp(selectedSpawnIndex, 0, spawnTypes.Length - 1);
                pool.SpawnPlatform(spawnTypes[clampedIndex]);
            }
        }
    }
}