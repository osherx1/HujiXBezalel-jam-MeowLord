using Game.Core.Input;
using Game.Core.Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core.Camera.Scripts
{
    public class HybridCameraFollow : MonoBehaviour
    {
        public Transform player;
        public float panSpeed = 10f;
        public float returnSpeed = 15f;
        public float safeZoneWidthPercent = 0.5f;
        public float safeZoneHeightPercent = 0.5f;

        private bool edgePanning = false;
        private bool isCameraLocked = false;

        void OnEnable()
        {
            InputSystemSingleton.Instance.InputSystem.PlayerControls.Lock.performed += OnLockPerformed;
            GameEvents.OnPlayerMoved += OnLockPerformed;
        }

        void OnDisable()
        {
            InputSystemSingleton.Instance.InputSystem.PlayerControls.Lock.performed -= OnLockPerformed;
            GameEvents.OnPlayerMoved -= OnLockPerformed;
        }

        private void OnLockPerformed(InputAction.CallbackContext ctx)
        {
            isCameraLocked = !isCameraLocked;
        }
        private void OnLockPerformed()
        {
            isCameraLocked = false;
        }

        void Update()
        {
            if (isCameraLocked)
                return;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float safeZoneWidth = screenWidth * safeZoneWidthPercent;
            float safeZoneHeight = screenHeight * safeZoneHeightPercent;

            Rect safeZone = new Rect(
                (screenWidth - safeZoneWidth) / 2f,
                (screenHeight - safeZoneHeight) / 2f,
                safeZoneWidth,
                safeZoneHeight
            );

            Vector3 mousePos = UnityEngine.Input.mousePosition;

            // If the mouse is outside the safe zone, edge pan
            if (!safeZone.Contains(mousePos))
            {
                edgePanning = true;
                Vector2 direction = ((Vector2)mousePos - safeZone.center).normalized;
                transform.position += new Vector3(direction.x, direction.y, 0) * panSpeed * Time.deltaTime;
            }
            else
            {
                edgePanning = false;
            }

            // If not edge-panning, smoothly return to follow the player
            if (!edgePanning && player != null)
            {
                Vector3 targetPos = new Vector3(player.position.x, player.position.y, transform.position.z);
                transform.position = Vector3.Lerp(transform.position, targetPos, returnSpeed * Time.deltaTime);
            }
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float safeZoneWidth = screenWidth * safeZoneWidthPercent;
            float safeZoneHeight = screenHeight * safeZoneHeightPercent;

            Rect safeZone = new Rect(
                (screenWidth - safeZoneWidth) / 2f,
                (screenHeight - safeZoneHeight) / 2f,
                safeZoneWidth,
                safeZoneHeight
            );

            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (!cam) return;

            Vector3[] screenCorners = new Vector3[4];
            screenCorners[0] = new Vector3(safeZone.xMin, safeZone.yMin, cam.nearClipPlane);
            screenCorners[1] = new Vector3(safeZone.xMin, safeZone.yMax, cam.nearClipPlane);
            screenCorners[2] = new Vector3(safeZone.xMax, safeZone.yMax, cam.nearClipPlane);
            screenCorners[3] = new Vector3(safeZone.xMax, safeZone.yMin, cam.nearClipPlane);

            for (int i = 0; i < 4; i++)
            {
                screenCorners[i] = cam.ScreenToWorldPoint(screenCorners[i]);
            }

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(screenCorners[0], screenCorners[1]);
            Gizmos.DrawLine(screenCorners[1], screenCorners[2]);
            Gizmos.DrawLine(screenCorners[2], screenCorners[3]);
            Gizmos.DrawLine(screenCorners[3], screenCorners[0]);
        }
    }
}
