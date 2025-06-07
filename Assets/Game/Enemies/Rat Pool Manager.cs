using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemies
{
    public class RatPoolManager : MonoBehaviour
    {
        public static RatPoolManager Instance;

        [SerializeField] private GameObject ratPrefab;
        [SerializeField] private int poolSize = 20;
        [SerializeField] private int maxActiveRats = 5;

        private readonly Queue<GameObject> _ratPool = new Queue<GameObject>();
        private int _activeRatCount;

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
            if (_activeRatCount >= maxActiveRats)
            {
                return null;
            }

            var rat = _ratPool.Count > 0 ? _ratPool.Dequeue() : Instantiate(ratPrefab);

            rat.SetActive(true);
            _activeRatCount++;
            return rat;
        }

        public void ReturnRat(GameObject rat)
        {
            rat.SetActive(false);
            _ratPool.Enqueue(rat);
            _activeRatCount = Mathf.Max(0, _activeRatCount - 1); // protect against double-return
        }

        public int GetActiveRatCount() => _activeRatCount;
    }
}