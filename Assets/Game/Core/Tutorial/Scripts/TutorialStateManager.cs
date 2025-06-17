using System;
using Game.Core.Camera.Scripts;
using UnityEngine;
using System.Collections;
using DG.Tweening;
using Game.Core.Input;
using Game.Core.Managers;
using Game.Enemies.Scripts;
using Game.Platforms.Scripts;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Game.Core.Tutorial
{
    public class TutorialStateManager : MonoBehaviour
    {
        [SerializeField] private HybridCameraFollow hybridCamera;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Transform playerPosition;
        [SerializeField] private Transform firstPositionOfCamera;
        [SerializeField] private GameObject mousePrefab;
        [SerializeField] private Transform startingMouseLocation;
        [SerializeField] private TutorialTextRenderer tutorialTextRenderer;
        private bool _mouseCatch = false;


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


        private void StartTutorialStateOrder()
        {
            StartCoroutine(StartOfTutorialState());
        }

        private void OnEnable()
        {
            GameEvents.OnTutorialStarted += StartTutorialStateOrder;
        }

        private void OnDisable()
        {
            InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
            InputSystemSingleton.Instance.InputSystem.PlayerControls.RightClick.performed -= OnRightClickPerformed;
            GameEvents.OnMouseCatch -= MouseCatch;
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

        [Header("FirstMouseTextState")] [TextArea(3, 10)] [SerializeField]
        private string firstMouseText;

        private IEnumerator FirstMouseTextState(GameObject rat)
        {
            tutorialTextRenderer.ShowBlurAndText(firstMouseText,
                () =>
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

        [Header("FirstMouseCatchState")] [SerializeField]
        private float delayPlayerPause = 0.2f;


        private IEnumerator FirstMouseCatchState()
        {
            GameEvents.OnMouseCatch += MouseCatch;

            while (true)
            {
                yield return null;
                if (_mouseCatch)
                {
                    _mouseCatch = false;
                    GameEvents.PlayerPause();
                    GameEvents.OnMouseCatch -= MouseCatch;
                    break;
                }
            }

            StartCoroutine(CameraMoveToSecondPlacementState());
        }

        public void MouseCatch(Vector3 mousePosition)
        {
            _mouseCatch = true;
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

        [Header("CameraMoveTextState")] [TextArea(3, 10)] [SerializeField]
        private string cameraMoveText;

        [SerializeField] private float cameraWaitTime = 2f;
        [SerializeField] private float triggerDistanceCameraMove = 0.5f;

        private IEnumerator CameraMoveTextState()
        {
            yield return new WaitForSeconds(cameraWaitTime);
            tutorialTextRenderer.ShowBlurAndText(cameraMoveText, () => { hybridCamera.isCameraLocked = false; });
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

        [Header("PlayerRightClickTextState")] [TextArea(3, 10)] [SerializeField]
        private string rightClickText;

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
                    InputSystemSingleton.Instance.InputSystem.PlayerControls.RightClick.performed -=
                        OnRightClickPerformed;
                    _rightClick = false;
                    break;
                }

                yield return null;
            }

            StartCoroutine(ServantEnterState());
        }

        #endregion

        #region ServantEnterState

        [SerializeField] private PlatformWaypointPoint waypointPointServantEnter;

        private IEnumerator ServantEnterState()
        {
            GameEvents.SpawnPlatform(PlatformType.ServantRed);
            while (true)
            {
                Debug.Log("Enter");
                yield return null;
                if (waypointPointServantEnter.pointOcuupied)
                {
                    break;
                }
            }

            StartCoroutine(ServantStopedMovingState());
        }

        #endregion

        #region ServantStopedMovingState

        [Header("ServantStopedMovingState")] [TextArea(3, 10)] [SerializeField]
        private string servantStopedMovingText;

        [TextArea(3, 10)] [SerializeField] private string yarnText;

        // ReSharper disable Unity.PerformanceAnalysis
        private IEnumerator ServantStopedMovingState()
        {
            tutorialTextRenderer.ShowBlurAndText(servantStopedMovingText,
                () => InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed += OnLeftClickPerformed);
            while (!_leftClick)
            {
                yield return null;
            }

            InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
            _leftClick = false;
            tutorialTextRenderer.TransitionText(yarnText,
                () => InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed += OnLeftClickPerformed);
            while (!_leftClick)
            {
                yield return null;
            }

            InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
            _leftClick = false;

            tutorialTextRenderer.HideBlurAndText((() =>
            {
                waypointPointServantEnter.stopForever = false;
                var rat = Instantiate(mousePrefab, firstPositionOfCamera.position, Quaternion.identity);
                rat.transform.position = startingMouseLocation.position;
                rat.transform.rotation = startingMouseLocation.rotation;
                GameEvents.PlayerResume();
            }));

            hybridCamera.RegisterCameraToGoBackToPlayer();
            hybridCamera.RegisterCameraToMoveTowardsPlayer();
            GameEvents.OnMouseCatch += MouseCatch;
            while (true)
            {
                yield return null;
                if (_mouseCatch)
                {
                    _mouseCatch = false;
                    GameEvents.OnMouseCatch -= MouseCatch;
                    hybridCamera.RegisterCameraToGoBackToPlayer(false);
                    hybridCamera.RegisterCameraToMoveTowardsPlayer(false);
                    hybridCamera.MoveTowardsPlayer();
                    GameEvents.PlayerPause();

                    break;
                }
            }

            StartCoroutine(YarnTextState());
        }

        #endregion

        #region YarnTextState

        [TextArea(3, 10)] [SerializeField] private string yarnAddedText;
        [TextArea(3, 10)] [SerializeField] private string queenText;
        [TextArea(3, 10)] [SerializeField] private string finalText;
        [TextArea(3, 10)] [SerializeField] private string greatJobText;


        [SerializeField] private PlatformWaypointPoint waypointPointServantEnd;
        [SerializeField] private PlatformWaypointPoint wayPointQueen;
        [SerializeField] private GameObject mouseTutorial2;

        private IEnumerator YarnTextState()
        {
            tutorialTextRenderer.ShowBlurAndText(yarnAddedText,
                () => InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed += OnLeftClickPerformed);
            while (!_leftClick)
            {
                yield return null;
            }

            InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
            _leftClick = false;
            tutorialTextRenderer.HideBlurAndText((() => GameEvents.SpawnPlatform(PlatformType.Queen)));
            while (!wayPointQueen.pointOcuupied)
            {
                yield return null;
            }

            tutorialTextRenderer.ShowBlurAndText(queenText,
                () => InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed += OnLeftClickPerformed);
            while (!_leftClick)
            {
                yield return null;
            }

            InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
            _leftClick = false;
            tutorialTextRenderer.TransitionText(finalText,
                () => InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed += OnLeftClickPerformed);
            while (!_leftClick)
            {
                yield return null;
            }
            InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
            _leftClick = false;
            tutorialTextRenderer.HideBlurAndText((() =>
            {
                waypointPointServantEnd.stopForever = false;
                var rat = Instantiate(mouseTutorial2, firstPositionOfCamera.position, Quaternion.identity);
                rat.transform.position = startingMouseLocation.position;
                rat.transform.rotation = startingMouseLocation.rotation;
                wayPointQueen.stopForever = false;
                GameEvents.OnMouseCatch += MouseCatch;
                GameEvents.PlayerResume();
                hybridCamera.RegisterCameraToGoBackToPlayer();
                hybridCamera.RegisterCameraToMoveTowardsPlayer();
            }));
            while (!_mouseCatch)
            {
                GameEvents.SpawnPlatform(PlatformType.ServantRed);
                yield return new WaitForSeconds(1f);
                GameEvents.SpawnPlatform(PlatformType.Queen);
            }
            _mouseCatch = false;
            GameEvents.PlayerPause();
            hybridCamera.RegisterCameraToGoBackToPlayer(false);
            hybridCamera.RegisterCameraToMoveTowardsPlayer(false);
            GameEvents.OnMouseCatch -= MouseCatch;
            tutorialTextRenderer.ShowBlurAndText(greatJobText,
                () => InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed += OnLeftClickPerformed);
            while (!_leftClick)
            {
                yield return null;
            }
            InputSystemSingleton.Instance.InputSystem.PlayerControls.Click.performed -= OnLeftClickPerformed;
            _leftClick = false;
            tutorialTextRenderer.HideBlurAndText(()=>SceneManager.LoadScene(2));
        }
        #endregion
    }
}