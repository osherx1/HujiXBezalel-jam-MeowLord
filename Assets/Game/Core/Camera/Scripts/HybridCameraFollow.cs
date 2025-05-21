using System.Collections.Generic;
using DG.Tweening;
using Game.Core.Input;
using Game.Core.Managers;
using Game.Core.Utils;
using Game.Player.Scripts;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core.Camera.Scripts
{
    public class HybridCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private float panSpeed = 10f;
        [SerializeField] private float returnSpeed = 15f;
        [SerializeField] private float goBackToCenterZoneWidthPercent = 0.5f;
        [SerializeField] private float goBackToCenterZoneHeightPercent = 0.5f;
        [SerializeField] private float dontMoveZoneHeightPercent = 0.7f;
        [SerializeField] private float dontMoveZoneWidthPercent = 0.7f;
        
        [SerializeField] private CinemachineTargetGroup targetGroup;
        [SerializeField] private CameraLogger cameraLogger;
        
        
        
        [SerializeField] private SpriteRenderer backgroundRenderer;
        
        private Tween cameraMoveTween;  
        private bool edgePanning = false;
        private bool isCameraLocked = false;
        [SerializeField] private bool logMessages;
        private UnityEngine.Camera _cam;
        private Vector3 _camPosBefore;
        private float _noMovementTime = 0f;
        [SerializeField] private float cameraStuckThreshold = 0.3f;
        private bool _didEdgePan;

        private List<Transform> CurrentPlayerPlatforms => playerMovement.PlayerPlatforms;
        

        void Start()
        {
            _cam = UnityEngine.Camera.main;
            if (backgroundRenderer == null)
            {
                cameraLogger?.Log("Background Renderer not set");
            }
        }
        void OnEnable()
        {
            // InputSystemSingleton.Instance.InputSystem.PlayerControls.Lock.performed += OnLockPerformed;
            GameEvents.OnPlayerMoved += OnPlayerMoved;
        }
        //
        // void OnDisable()
        // {
        //     InputSystemSingleton.Instance.InputSystem.PlayerControls.Lock.performed -= OnLockPerformed;
        //     GameEvents.OnPlayerMoved -= OnPlayerMoved;
        // }
        //
        // private void OnLockPerformed(InputAction.CallbackContext ctx)
        // {
        //     isCameraLocked = !isCameraLocked;
        // }
        private void OnPlayerMoved()
        {
            if (player != null)
            {
                // Kill any existing tween before starting a new one
                cameraMoveTween?.Kill();

                cameraMoveTween = transform.DOMove(player.position, 2f)
                    .SetEase(Ease.OutQuad);
            }
        }

        void Update()
        {
            if (isCameraLocked)
                return;

            Vector3 mousePos = UnityEngine.Input.mousePosition;
            Rect dontMoveRect = EladsHelperFunctions.GetCenteredRect(dontMoveZoneWidthPercent, dontMoveZoneHeightPercent);

            // Calculate camera's current position before moving the target
            _camPosBefore = UnityEngine.Camera.main.transform.position;

            _didEdgePan = false;

            // --- EDGE PAN LOGIC ---
            if (!dontMoveRect.Contains(mousePos))
            {
                // Try to pan the camera target
                edgePanning = true;
                Vector2 direction = ((Vector2)mousePos - dontMoveRect.center).normalized;
                transform.position += new Vector3(direction.x, direction.y, 0) * (panSpeed * Time.deltaTime);
                _didEdgePan = true;
            }
            else
            {
                _didEdgePan = false;
                edgePanning = false;
            }

            // Clamp the camera target to background bounds (optional, for safety)
            transform.position = EladsHelperFunctions.ClampPositionToBounds(backgroundRenderer.bounds, transform.position);

            // Wait until after Cinemachine processes camera position (usually LateUpdate), but for simplicity:
            // Let's check IMMEDIATELY if the camera moved (for most setups this is sufficient, but see note below!)
           

            
        }

        
        private void LateUpdate()
        {
            Vector3 camPosAfter = UnityEngine.Camera.main.transform.position;

            // Check if edge-panning was attempted and camera didn't move
            if (camPosAfter == _camPosBefore)
            {
                _noMovementTime += Time.deltaTime;
                if (_noMovementTime > cameraStuckThreshold)
                {
                    // Snap camera target to center
                    Vector3 cameraWorldCenter = UnityEngine.Camera.main.transform.position;
                    cameraWorldCenter.z = transform.position.z;
                    transform.position = cameraWorldCenter;
                    cameraLogger?.Log("Camera target snapped to center after being stuck.");
                    _noMovementTime = 0; // Reset timer so it doesn't keep snapping
                }
            }
            else
            {
                // Camera moved, so reset timer
                _noMovementTime = 0;
            }
       

            // 1. Get the platforms you want to show in the camera (e.g., playerMovement.PlayerPlatforms)
            var platformsToShow = playerMovement.PlayerPlatforms;

            // 2. Clear the old targets
            targetGroup.Targets.Clear();

            // 3. Add each platform to the group
            foreach (var t in platformsToShow)
            {
                if (t != null)
                    targetGroup.Targets.Add(new CinemachineTargetGroup.Target { Object = t, Weight = 1, Radius = 0 });
            }

            // 4. (Optional) Also add the player if not already in the list
            if (!platformsToShow.Contains(player))
                targetGroup.Targets.Add(new CinemachineTargetGroup.Target { Object = player, Weight = 1, Radius = 0 });
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
