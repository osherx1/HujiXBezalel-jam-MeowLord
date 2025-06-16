using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Generics
{
    public class MonoPool<T> : MonoSingleton<MonoPool<T>> where T : MonoBehaviour, IPoolable
    {
        [SerializeField] private int initialSize = 10;
        [SerializeField] private T prefab;
        [SerializeField] private Transform parent;
        private Stack<T> _pool;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(parent);
            _pool = new Stack<T>();
            CreateObjects();
        }
        public T Get()
        {
            if (_pool.Count == 0)
            {
                CreateObjects();
            }
            T obj = _pool.Pop();
            obj.Reset();
            obj.gameObject.SetActive(true);
            return obj;
        }

        public void Return(T obj)
        {
            obj.gameObject.SetActive(false);
            _pool.Push(obj);
        }

        private void CreateObjects()
        {
            for (int i = 0; i < initialSize; i++)
            {
                var obj = Instantiate(prefab, parent);
                obj.gameObject.SetActive(false);
                _pool.Push(obj);
            }
        }
    
    }
}
