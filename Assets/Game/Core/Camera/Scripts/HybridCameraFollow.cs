using Game.Core.Input;
using Game.Core.Managers;
using Game.Core.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core.Camera.Scripts
{
    public class HybridCameraFollow : MonoBehaviour
    {
        public Transform player;
        [SerializeField] private float panSpeed = 10f;
        [SerializeField] private float returnSpeed = 15f;
        [SerializeField] private float goBackToCenterZoneWidthPercent = 0.5f;
        [SerializeField] private float goBackToCenterZoneHeightPercent = 0.5f;
        [SerializeField] private float dontMoveZoneHeightPercent = 0.7f;
        [SerializeField] private float dontMoveZoneWidthPercent = 0.7f;
        
        
        [SerializeField] private SpriteRenderer backgroundRenderer;

        private bool edgePanning = false;
        private bool isCameraLocked = false;
        [SerializeField] private bool logMessages;

        void Start()
        {
            if (backgroundRenderer == null)
            {
                Debug.LogError("Background renderer is null!");
            }
        }
        void OnEnable()
        {
            InputSystemSingleton.Instance.InputSystem.PlayerControls.Lock.performed += OnLockPerformed;
            GameEvents.OnPlayerMoved += OnPlayerMoved;
        }

        void OnDisable()
        {
            InputSystemSingleton.Instance.InputSystem.PlayerControls.Lock.performed -= OnLockPerformed;
            GameEvents.OnPlayerMoved -= OnPlayerMoved;
        }

        private void OnLockPerformed(InputAction.CallbackContext ctx)
        {
            isCameraLocked = !isCameraLocked;
        }
        private void OnPlayerMoved()
        {
            isCameraLocked = false;
        }

        void Update()
        {
            if (isCameraLocked)
                return;

            Vector3 mousePos = UnityEngine.Input.mousePosition;

            Rect goBackRect = EladsHelperFunctions.GetCenteredRect(goBackToCenterZoneWidthPercent, goBackToCenterZoneHeightPercent);
            Rect dontMoveRect = EladsHelperFunctions.GetCenteredRect(dontMoveZoneWidthPercent, dontMoveZoneHeightPercent);

            // ZONE LOGIC ORDER: Innermost (go back), middle (don't move), outside (pan)
            if (goBackRect.Contains(mousePos))
            {
                edgePanning = false;
                // Go back to player
                if (player != null)
                {
                    Vector3 targetPos = new Vector3(player.position.x, player.position.y, transform.position.z);
                    transform.position = Vector3.Lerp(transform.position, targetPos, returnSpeed * Time.deltaTime);
                }
            }
            else if (dontMoveRect.Contains(mousePos))
            {
                // Do nothing: neither edge pan nor return to player
                edgePanning = false;
            }
            else
            {
                // Outside: edge pan!
                if (!EladsHelperFunctions.IsWithinBoundsXY(backgroundRenderer.bounds, transform.position))
                {
                    Logger("Position is out of bounds");
                    return;
                }
                edgePanning = true;
                Vector2 direction = ((Vector2)mousePos - dontMoveRect.center).normalized;
                transform.position += new Vector3(direction.x, direction.y, 0) * (panSpeed * Time.deltaTime);
            }
        }

        private void Logger(string message)
        {
            if(logMessages) Debug.Log(message);
        }


        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (!cam) return;

            // Draw goBackToCenter zone
            DrawScreenRectGizmo(EladsHelperFunctions.GetCenteredRect(goBackToCenterZoneWidthPercent, goBackToCenterZoneHeightPercent), cam, Color.green);

            // Draw dontMove zone
            DrawScreenRectGizmo(EladsHelperFunctions.GetCenteredRect(dontMoveZoneWidthPercent, dontMoveZoneHeightPercent), cam, Color.yellow);
        }

        private void DrawScreenRectGizmo(Rect rect, UnityEngine.Camera cam, Color color)
        {
            Vector3[] corners = new Vector3[4];
            corners[0] = cam.ScreenToWorldPoint(new Vector3(rect.xMin, rect.yMin, cam.nearClipPlane));
            corners[1] = cam.ScreenToWorldPoint(new Vector3(rect.xMin, rect.yMax, cam.nearClipPlane));
            corners[2] = cam.ScreenToWorldPoint(new Vector3(rect.xMax, rect.yMax, cam.nearClipPlane));
            corners[3] = cam.ScreenToWorldPoint(new Vector3(rect.xMax, rect.yMin, cam.nearClipPlane));

            Gizmos.color = color;
            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
            Gizmos.DrawLine(corners[3], corners[0]);
        }

    }
}
