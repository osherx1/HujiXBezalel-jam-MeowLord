using System.Collections;
using System.Collections.Generic;
using Attributes;
using DG.Tweening;
using Game.Core.Input;
using Game.Core.Managers;
using Game.Core.Utils;
using Game.Player.Scripts;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

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
        [SerializeField] private CinemachineGroupFraming targetFraming;
        [SerializeField] [Range(0, 11)] private float startingFrameSize;
        [SerializeField] private int maxPlatformsForFrameShrinking = 6;
        [SerializeField] private float minFrameSize = 0.05f; 
        
        [SerializeField] private TargetLogger targetLogger;
        
        
        [Header("Background")]
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private BoxCollider2D backgroundCollider2D;
        // Assign these in the Inspector for full control
        [SerializeField] private Vector2[] colliderSizes; 
        [SerializeField] private Vector2[] colliderSizeOffsets; 
        [SerializeField] private CinemachineConfiner2D cinemachineConfiner;

        [Header("UI Exclusion Zone")]
        [SerializeField] private Rect uiExclusionZone = new Rect(20, 20, 180, 60); // Default values, adjust in Inspector
        
        private Tween cameraMoveTween;  
        private bool edgePanning = false;
        [SerializeField] public bool isCameraLocked = true;
        [SerializeField] public int tutorialModeTargetFrame = 0;

        

        private UnityEngine.Camera _cam;
        private Vector3 _camPosBefore;
        private float _noMovementTime = 0f;
        [SerializeField] private float cameraStuckThreshold = 0.1f;
        private bool _didEdgePan;
        private int _clampedPlatform = 5;

        private List<Transform> CurrentPlayerPlatforms => playerMovement.PlayerPlatforms;
        
        void Start()
        {
            _cam = UnityEngine.Camera.main;
            if (backgroundRenderer == null)
            {
                targetLogger?.Log("Background Renderer not set");
            }
        }
        void OnEnable()
        {
            targetLogger?.Log("Target subscribed to player");
            GameEvents.OnGameStarted += EnableOnGameStart;
        }

        private void EnableOnGameStart()
        {
            isCameraLocked = false;
            RegisterCameraToGoBackToPlayer();
            RegisterCameraToAdjustFraming();
        }

        public void RegisterCameraToGoBackToPlayer(bool register = true)
        {
            if (register)
            {
                InputSystemSingleton.Instance.InputSystem.PlayerControls.RightClick.performed += OnRightClickPerformed;
            }
            else
            {
                InputSystemSingleton.Instance.InputSystem.PlayerControls.RightClick.performed -= OnRightClickPerformed;
            }
        }

        public void RegisterCameraToAdjustFraming(bool register = true)
        {
            if (register)
            {
                GameEvents.OnPlayerLanded += AdjustTargetFraming;
            }
            else
            {
                GameEvents.OnPlayerLanded -= AdjustTargetFraming;
            }
        }

        public void RegisterCameraToMoveTowardsPlayer(bool register = true)
        {
            if (register)
            {
                GameEvents.OnPlayerLanded += MoveTowardsPlayer;
            }
            else
            {
                GameEvents.OnPlayerLanded -= MoveTowardsPlayer;
            }
        }

        void OnDisable()
        {
            RegisterCameraToGoBackToPlayer(false);
            RegisterCameraToMoveTowardsPlayer(false);
            RegisterCameraToAdjustFraming(false);
        }

        private void OnRightClickPerformed(InputAction.CallbackContext obj)
        {
            MoveTowardsPlayer();
        }
        
        public void MoveTowardsPlayer()
        {
            targetLogger?.Log("Target Entered Player moved Function");
            if (player != null)
            {
                // Kill any existing tween before starting a new one
                cameraMoveTween?.Kill();
                targetLogger?.Log("Camera Moved Towards Player");
                cameraMoveTween = transform.DOMove(player.position, 2f)
                    .SetEase(Ease.OutQuad);
            }
        }

        void Update()
        {
            if(isCameraLocked) return;
            
            _camPosBefore = UnityEngine.Camera.main.transform.position;

            float moveX = UnityEngine.Input.GetAxisRaw("Horizontal");
            float moveY = UnityEngine.Input.GetAxisRaw("Vertical");
            Vector3 move = new Vector3(moveX, moveY, 0).normalized;
            if (move.sqrMagnitude > 0)
            {
                cameraMoveTween?.Kill();
                transform.position += move * (panSpeed * Time.deltaTime);
            }
            
            // Vector3 mousePos = UnityEngine.Input.mousePosition;
            // Rect dontMoveRect = EladsHelperFunctions.GetCenteredRect(dontMoveZoneWidthPercent, dontMoveZoneHeightPercent);
            //
            // // Check if mouse is inside the UI exclusion zone (screen coordinates)
            // if (uiExclusionZone.Contains(new Vector2(mousePos.x, Screen.height - mousePos.y))) // Y flip for screen coords
            // {
            //     _didEdgePan = false;
            //     edgePanning = false;
            //     return;
            // }
            //
            // // Calculate camera's current position before moving the target
            // _camPosBefore = UnityEngine.Camera.main.transform.position;
            // 
            // _didEdgePan = false;
            //
            // // --- EDGE PAN LOGIC ---
            // if (!dontMoveRect.Contains(mousePos))
            // {
            //     // Try to pan the camera target
            //     cameraMoveTween?.Kill();
            //     Vector2 direction = ((Vector2)mousePos - dontMoveRect.center).normalized;
            //     transform.position += new Vector3(direction.x, direction.y, 0) * (panSpeed * Time.deltaTime);
            //   
            // }
            // else
            // {
            //     _didEdgePan = false;
            //     edgePanning = false;
            // }
            
        }

        
        private void LateUpdate()
        {
            if(isCameraLocked) return;
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
                    _noMovementTime = 0; // Reset timer so it doesn't keep snapping
                }
            }
            else
            {
                // Camera moved, so reset timer
                _noMovementTime = 0;
            }
            
            
        }

        public void AdjustTargetFraming()
        {
            int numPlatforms =  tutorialModeTargetFrame != 0 ? tutorialModeTargetFrame : CurrentPlayerPlatforms.Count;
            
            // If 0 or 1, keep the starting frame size
            if (numPlatforms <= 1)
            {
                targetFraming.FramingSize = startingFrameSize;
            }
            else
            {
               
                float t = Mathf.Clamp01((float)(numPlatforms - 1) / (maxPlatformsForFrameShrinking - 1));
                targetFraming.FramingSize = Mathf.Lerp(startingFrameSize, minFrameSize, t);
            }
            int clampedPlatforms = Mathf.Clamp(numPlatforms-1, 0, colliderSizes.Length - 1);
            if (_clampedPlatform != clampedPlatforms)
            {
                backgroundCollider2D.size = colliderSizes[clampedPlatforms];
                backgroundCollider2D.offset = colliderSizeOffsets[clampedPlatforms];
                cinemachineConfiner.InvalidateBoundingShapeCache();
                _clampedPlatform = clampedPlatforms;
                if(tutorialModeTargetFrame != 0) MoveTowardsPlayer();
            }
        }

        public void AdjustTargetFraming(System.Action onComplete)
        {
            AdjustTargetFraming();
            MoveTowardsPlayer();
            if (onComplete != null)
            {
                StartCoroutine(InvokeAfterDelayCoroutine(onComplete, 1f));
            }
        }

        private IEnumerator InvokeAfterDelayCoroutine(System.Action callback, float delay)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
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

            // Draw UI exclusion zone (in screen space, red)
            Gizmos.color = Color.red;
            Vector3 p1 = cam.ScreenToWorldPoint(new Vector3(uiExclusionZone.xMin, Screen.height - uiExclusionZone.yMin, cam.nearClipPlane));
            Vector3 p2 = cam.ScreenToWorldPoint(new Vector3(uiExclusionZone.xMax, Screen.height - uiExclusionZone.yMin, cam.nearClipPlane));
            Vector3 p3 = cam.ScreenToWorldPoint(new Vector3(uiExclusionZone.xMax, Screen.height - uiExclusionZone.yMax, cam.nearClipPlane));
            Vector3 p4 = cam.ScreenToWorldPoint(new Vector3(uiExclusionZone.xMin, Screen.height - uiExclusionZone.yMax, cam.nearClipPlane));
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);
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
