using UnityEngine;
using Game.Platforms.Scripts;

public class PlatformSpawner : MonoBehaviour
{
    [Header("Reference to the Pool")]
    public MovingPlatformPool pool;

    [Header("Platform type to spawn")]
    public PlatformType spawnType = PlatformType.ServantRed;

    [Header("Spawn every N seconds")]
    public float spawnInterval = 5f;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            if (pool != null)
                pool.SpawnPlatform(spawnType);
        }
    }
}