using System;
using System.Collections.Generic;
using Game.Core.Generics;
using UnityEngine;

namespace Game.Core.Managers
{
    public class UnityMainThreadDispatcher : MonoSingleton<UnityMainThreadDispatcher>
    {
        private readonly Queue<Action> _executionQueue = new Queue<Action>();

        void Update()
        {
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                {
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }

        public void Enqueue(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            Debug.Log("Enter UnityMainThreadDispatcher");
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }

        public void StartObject()
        {
            Debug.Log("UnityMainThreadDispatcher created");
        }
    }
} 