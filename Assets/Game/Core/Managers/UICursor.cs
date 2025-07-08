using Game.Core.Managers;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UI
{
    public class UICursor : MonoBehaviour
    {
        public enum CursorState
        {
            Normal,
            Hover,
            Active
        }

        [Tooltip("Drag your UI Image’s RectTransform here")]
        public RectTransform cursorRect;

        [Tooltip("Drag your UI Image component here")]
        public Image cursorImage;

        [Header("Sprites for Cursor States")]
        public Sprite normalSprite;
        public Sprite hoverSprite;
        public Sprite activeSprite;

        [Header("Set the platform LayerMask (2D)")]
        public LayerMask platformLayerMask;

        private CursorState currentState = CursorState.Normal;
        private bool isHovering = false;

        void Awake()
        {
            Cursor.visible = false;
            SetState(CursorState.Normal);
        }

        void Update()
        {
            if (cursorRect == null) return;
            cursorRect.position = Input.mousePosition;

            // UI interaction first
            CursorState uiState = GetUICursorState();
            if (uiState != CursorState.Normal)
            {
                SetState(uiState);
                return;
            }

            // Then, check platform hover via event
            if (isHovering)
            {
                if (Input.GetMouseButton(0))
                    SetState(CursorState.Active);
                else
                    SetState(CursorState.Hover);
            }
            else
            {
                SetState(CursorState.Normal);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnPlatformHover += HandlePlatformHover;
        }
        private void OnDisable()
        {
            GameEvents.OnPlatformHover -= HandlePlatformHover;
        }
        private void HandlePlatformHover(bool hovering)
        {
            isHovering = hovering;
        }

        // Check if hovering or clicking on any interactive UI element
        private CursorState GetUICursorState()
        {
            if (EventSystem.current == null)
                return CursorState.Normal;

            if (EventSystem.current.IsPointerOverGameObject())
            {
                var pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                var results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                foreach (var r in results)
                {
                    // Check if UI element is interactive
                    if (r.gameObject.GetComponent<Button>() != null ||
                        r.gameObject.GetComponent<Toggle>() != null ||
                        r.gameObject.GetComponent<UnityEngine.UI.InputField>() != null ||
                        r.gameObject.GetComponent<UnityEngine.UI.Slider>() != null ||
                        r.gameObject.GetComponent<UnityEngine.UI.Dropdown>() != null)
                    {
                        if (Input.GetMouseButton(0))
                            return CursorState.Active;
                        else
                            return CursorState.Hover;
                    }
                }
            }
            return CursorState.Normal;
        }

        public void SetState(CursorState state)
        {
            if (currentState == state) return;
            currentState = state;

            if (cursorImage == null) return;

            switch (state)
            {
                case CursorState.Normal:
                    cursorImage.sprite = normalSprite;
                    break;
                case CursorState.Hover:
                    cursorImage.sprite = hoverSprite;
                    break;
                case CursorState.Active:
                    cursorImage.sprite = activeSprite;
                    break;
            }
        }
    }
}
