using UnityEngine;
using System.Collections;

namespace Game.Core.Utils
{
    public class TimerProxy : MonoBehaviour
    {
        public Coroutine StartRoutine(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }
    }
} 