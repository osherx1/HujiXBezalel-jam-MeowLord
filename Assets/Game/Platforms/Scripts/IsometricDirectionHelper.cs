using UnityEngine;

namespace Game.Platforms.Scripts
{
    [ExecuteInEditMode]
    public class IsometricDirectionHelper : MonoBehaviour
    {
        [Header("Right Direction (start, end)")]
        public Transform rightStart;
        public Transform rightEnd;
        [Header("Up Direction (start, end)")]
        public Transform upStart;
        public Transform upEnd;

        [Header("Preview (readonly)")]
        public Vector2 rightDir;
        public Vector2 leftDir;
        public Vector2 upDir;
        public Vector2 downDir;

        private static IsometricDirectionHelper _instance;

        void Awake()
        {
            _instance = this;
            UpdateDirections();
        }

        void Update()
        {
            UpdateDirections();
        }

        private void UpdateDirections()
        {
            // Right
            if (rightStart && rightEnd)
                rightDir = (rightEnd.position - rightStart.position).normalized;
            else
                rightDir = Vector2.right;

            leftDir = -rightDir;

            // Up
            if (upStart && upEnd)
                upDir = (upEnd.position - upStart.position).normalized;
            else
                upDir = Vector2.up;

            downDir = -upDir;
        }
    
        public static Vector2 RightDirection => _instance ? _instance.rightDir : Vector2.right;
        public static Vector2 LeftDirection  => _instance ? _instance.leftDir  : Vector2.left;
        public static Vector2 UpDirection    => _instance ? _instance.upDir    : Vector2.up;
        public static Vector2 DownDirection  => _instance ? _instance.downDir  : Vector2.down;
    }
}