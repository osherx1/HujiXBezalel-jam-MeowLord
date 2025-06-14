using System;
using Game.Core.Camera.Scripts;
using UnityEngine;
using System.Collections;
using DG.Tweening;
using Game.Core.Input;
using Game.Core.Managers;
using Game.Enemies.Scripts;
using TMPro;
using UnityEngine.InputSystem;

namespace Game.Core.Tutorial
{
    public class TutorialStateManager: MonoBehaviour
    {
        [SerializeField] private HybridCameraFollow hybridCamera;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Transform playerPosition;
        [SerializeField] private Transform firstPositionOfCamera;
        [SerializeField] private GameObject mousePrefab;
        [SerializeField] private Transform startingMouseLocation;
        [SerializeField] private TutorialTextRenderer tutorialTextRenderer;
        
        
        
        
        #region TutorialStateOrder
        // 1. StartOfTutorial
        // 2. FirstMouseEnter
        // 3. FirstMouseText
        // 4. FirstMouseCatch
        // 5. CameraMoveToSecondPlacement
        // 6. CameraMoveText
        // 7. CameraMovedByPlayer
        // 8. PlayerLeftClickText
        // 9. PlayerLeftClicked
        // 10. ServantKingAndQueenEnter
        // 11. SecondMouseEnter
        // 12. YarnText
        // 13. WhileCatchingSecondMouse
        // 14. SecondMosueCatch
        // 15. FinalText
        #endregion

        
        private void Start()
        {
            StartCoroutine(StartOfTutorialState());
        }

        private void OnDisable()
        {
            InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
            InputSystemSingleton.Instance.InputSystem.PlayerControls.RightClick.performed -= OnRightClickPerformed;
            GameEvents.OnMouseCatch -= FirstMouseCatch;
        }

        #region ClickInput
        private bool _rightClick = false;
        private bool _leftClick = false;
        private void OnLeftClickPerformed(InputAction.CallbackContext obj)
        {
            _leftClick = true;
        }

        private void OnRightClickPerformed(InputAction.CallbackContext obj)
        {
            _rightClick = true;
        }
        
        #endregion
        
        #region StartOfTutorialState
        private IEnumerator StartOfTutorialState()
        {
            hybridCamera.RegisterCameraToGoBackToPlayer(false);
            hybridCamera.RegisterCameraToMoveTowardsPlayer(false);
            hybridCamera.RegisterCameraToAdjustFraming(false);
            GameEvents.PlayerPause();
            cameraTarget.position = firstPositionOfCamera.position;
            
            while (true)
            {
                yield return null;
                break;
            }
            StartCoroutine(FirstMouseEnterState());
        }
        #endregion

        #region FirstMouseEnterState
        private IEnumerator FirstMouseEnterState()
        {
            var rat = Instantiate(mousePrefab, firstPositionOfCamera.position, Quaternion.identity);
            rat.transform.position = startingMouseLocation.position;
            rat.transform.rotation = startingMouseLocation.rotation;
            var ratMovement = rat.GetComponentInChildren<RatTutorialMovement>();
            while (true)
            {
                yield return null;
                if (ratMovement.StopedMoving)
                {
                    break;
                }
            }
            StartCoroutine(FirstMouseTextState(rat));
        }
        #endregion

        #region FirstMouseTextState
        [Header("FirstMouseTextState")]
        [TextArea(3, 10)]  
        [SerializeField] private string firstMouseText;
        
        private IEnumerator FirstMouseTextState(GameObject rat)
        {
            tutorialTextRenderer.ShowBlurAndText(firstMouseText,() =>
            {
                InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed += OnLeftClickPerformed;
            }); 
            
            while (true)
            {
                yield return null;
                if (_leftClick)
                {
                    GameEvents.PlayerResume();
                    tutorialTextRenderer.HideBlurAndText();
                    _leftClick = false;
                    InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
                    break;
                }
            }
            StartCoroutine(FirstMouseCatchState());
        }
        #endregion

        #region FirstMouseCatchState
        private bool _firstMouseCatch = false;

        [Header("FirstMouseCatchState")] [SerializeField]
        private float delayPlayerPause = 0.2f;
        

        private IEnumerator FirstMouseCatchState()
        {
            GameEvents.OnMouseCatch += FirstMouseCatch;
            
            while (true)
            {
                yield return null;
                if (_firstMouseCatch)
                {
                    GameEvents.PlayerPause();
                    GameEvents.OnMouseCatch -= FirstMouseCatch;
                    break;
                }
            }
            StartCoroutine(CameraMoveToSecondPlacementState());
        }

        public void FirstMouseCatch(Vector3 mousePosition)
        {
            _firstMouseCatch = true;
        }
        #endregion

        #region CameraMoveToSecondPlacementState
        
        
        private IEnumerator CameraMoveToSecondPlacementState()
        {
            hybridCamera.MoveTowardsPlayer();
            hybridCamera.AdjustTargetFraming();
            yield return null;
            StartCoroutine(CameraMoveTextState());
        }
        #endregion

        #region CameraMoveTextState
        [Header("CameraMoveTextState")]
        [TextArea(3, 10)]  
        [SerializeField] private string cameraMoveText;
        [SerializeField] private float cameraWaitTime = 2f;
        [SerializeField] private float triggerDistanceCameraMove = 0.5f;
        private IEnumerator CameraMoveTextState()
        {
            yield return new WaitForSeconds(cameraWaitTime);
            tutorialTextRenderer.ShowBlurAndText(cameraMoveText, () =>
            {
                hybridCamera.isCameraLocked = false;
            });
            var lastPosition = cameraTarget.transform.position;
            float totalMovement = 0f;
            while (true)
            {
                var currentPosition = cameraTarget.transform.position;
                totalMovement += Vector3.Distance(currentPosition, lastPosition);
                lastPosition = currentPosition;
                if (totalMovement > triggerDistanceCameraMove)
                {
                    hybridCamera.isCameraLocked = true;
                    break;
                }
                yield return null;
            }
            StartCoroutine(PlayerRightClickTextState());
        }
        #endregion
        
        #region PlayerRightClickTextState
        [Header("PlayerRightClickTextState")]
        [TextArea(3, 10)]  
        [SerializeField] private string rightClickText;
        private IEnumerator PlayerRightClickTextState()
        {
            tutorialTextRenderer.TransitionText(rightClickText, () =>
            {
                InputSystemSingleton.Instance.InputSystem.PlayerControls.RightClick.performed += OnRightClickPerformed;
                hybridCamera.RegisterCameraToGoBackToPlayer();
            });
            while (true)
            {
                if (_rightClick)
                {
                    hybridCamera.RegisterCameraToGoBackToPlayer(false);
                    tutorialTextRenderer.HideBlurAndText();
                    InputSystemSingleton.Instance.InputSystem.PlayerControls.RightClick.performed -= OnRightClickPerformed;
                    _rightClick = false;
                    break;
                }
                yield return null;
            }
            StartCoroutine(ServantKingAndQueenEnterState());
        }
        #endregion
        
        #region ServantKingAndQueenEnterState
        private IEnumerator ServantKingAndQueenEnterState()
        {
            // Pre-logic for ServantKingAndQueenEnter
            // TODO: Add pre-logic here
            while (true)
            {
                yield return null;
                // TODO: Wait for condition to proceed to SecondMouseEnter
                if (false) // Replace with actual condition
                    break;
            }
            StartCoroutine(SecondMouseEnterState());
        }
        #endregion

        #region SecondMouseEnterState
        private IEnumerator SecondMouseEnterState()
        {
            // Pre-logic for SecondMouseEnter
            // TODO: Add pre-logic here
            while (true)
            {
                yield return null;
                // TODO: Wait for condition to proceed to YarnText
                if (false) // Replace with actual condition
                    break;
            }
            StartCoroutine(YarnTextState());
        }
        #endregion

        #region YarnTextState
        private IEnumerator YarnTextState()
        {
            // Pre-logic for YarnText
            // TODO: Add pre-logic here
            while (true)
            {
                yield return null;
                // TODO: Wait for condition to proceed to WhileCatchingSecondMouse
                if (false) // Replace with actual condition
                    break;
            }
            StartCoroutine(WhileCatchingSecondMouseState());
        }
        #endregion

        #region WhileCatchingSecondMouseState
        private IEnumerator WhileCatchingSecondMouseState()
        {
            // Pre-logic for WhileCatchingSecondMouse
            // TODO: Add pre-logic here
            while (true)
            {
                yield return null;
                // TODO: Wait for condition to proceed to SecondMosueCatch
                if (false) // Replace with actual condition
                    break;
            }
            StartCoroutine(SecondMosueCatchState());
        }
        #endregion

        #region SecondMosueCatchState
        private IEnumerator SecondMosueCatchState()
        {
            // Pre-logic for SecondMosueCatch
            // TODO: Add pre-logic here
            while (true)
            {
                yield return null;
                // TODO: Wait for condition to proceed to FinalText
                if (false) // Replace with actual condition
                    break;
            }
            StartCoroutine(FinalTextState());
        }
        #endregion

        #region FinalTextState
        private IEnumerator FinalTextState()
        {
            // Pre-logic for FinalText
            // TODO: Add pre-logic here
            while (true)
            {
                yield return null;
                // TODO: Wait for condition to finish tutorial
                if (false) // Replace with actual condition
                    break;
            }
            // Tutorial finished
        }
        #endregion
    }
}