using UnityEngine;

namespace Game.Platforms.Scripts
{
    public class PlatformWaypointPoint : MonoBehaviour
    {
        public bool stopAtPoint = false;
        public float stopDelay = 0f;
        
        [Tooltip("If true, the platform will stop forever at this point.")]
        public bool stopForever = false;
        
    }
}