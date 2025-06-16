using System.Collections;
using Game.Enemies.Scripts;
using UnityEngine;

namespace Game.Enemies
{
    public class RatSpawner : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnInterval = 2f;
        //Todo: add a disable to the spawner when the game ends via event

        [SerializeField] private GameObject score;
        
        private void Start()
        {
            StartCoroutine(SpawnRats());
        }

        private IEnumerator SpawnRats()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);

                // Pick a random spawn point
                Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

                // Get a rat from the pool
                GameObject rat = RatPoolManager.Instance.GetRat();
                if (rat != null)
                {
                    rat.transform.position = randomSpawnPoint.position;
                    rat.transform.rotation = randomSpawnPoint.rotation;
                    RatHealth ratHealth = rat.GetComponent<RatHealth>();
                    ratHealth.SetScoreTarget(score);
                    
                }
            }
        }
    }
}