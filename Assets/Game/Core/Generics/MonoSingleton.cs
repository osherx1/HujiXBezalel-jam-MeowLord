using UnityEngine;

namespace Game.Core.Generics
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isInitialized;

        public static T Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                // Try to find existing instance in the scene
                _instance = FindFirstObjectByType<T>();
                if (_instance != null && !_isInitialized)
                {
                    // We found a scene object but it hasn't gone through Awake yet.
                    // Force initialization
                    (_instance as MonoSingleton<T>)?.ForceInitialize();
                }

                // If still not found, create a new one
                if (_instance == null)
                {
                    var singletonObject = new GameObject(typeof(T).Name);
                    _instance = singletonObject.AddComponent<T>();
                    DontDestroyOnLoad(singletonObject);
                    _isInitialized = true;
                }

                return _instance;
            }
        }

        private void ForceInitialize()
        {
            if (!_isInitialized)
            {
                DontDestroyOnLoad(gameObject);
                _isInitialized = true;
            }
            
        }
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                _isInitialized = true;
            }
            else if (_instance != this)
            {
                Destroy(gameObject); // Duplicate
            }
        }
    }
}