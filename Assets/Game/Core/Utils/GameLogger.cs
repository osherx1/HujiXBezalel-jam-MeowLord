using UnityEngine;

namespace Game.Core.Utils
{
    public class GameLogger: MonoBehaviour
    {
        public bool active = true; // If false, no logs will be printed

        public void Log(string message)
        {
            if (!active) return;
            Debug.Log(message);
        }

        public void LogWarning(string message)
        {
            if (!active) return;
            Debug.LogWarning(message);
        }

        public void LogError(string message)
        {
            if (!active) return;
            Debug.LogError(message);
        }

        
    }
}