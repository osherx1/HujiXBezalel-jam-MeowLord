using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemies
{
    public class RatPoolManager : MonoBehaviour
    {
        public static RatPoolManager Instance;

        [SerializeField] private GameObject ratPrefab;
        [SerializeField] private int poolSize = 20;
        
        [SerializeField] private int maxPoolSize = 5;

        private readonly Queue<GameObject> _ratPool = new Queue<GameObject>();

        private void Awake()
        {
            Instance = this;
            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject rat = Instantiate(ratPrefab);
                rat.SetActive(false);
                _ratPool.Enqueue(rat);
            }
        }

        public GameObject GetRat()
        {
            if (_ratPool.Count >= maxPoolSize)
            {
                return null;
            }
            
            if (_ratPool.Count > 0)
            {
                GameObject rat = _ratPool.Dequeue();
                rat.SetActive(true);
                return rat;
            }

            // Optional: expand pool if needed
            GameObject newRat = Instantiate(ratPrefab);
            newRat.SetActive(true);
            return newRat;
        }

        public void ReturnRat(GameObject rat)
        {
            rat.SetActive(false);
            _ratPool.Enqueue(rat);
        }
    }
}
