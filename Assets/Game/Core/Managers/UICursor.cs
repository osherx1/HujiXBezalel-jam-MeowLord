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

        void Awake()
        {
            Cursor.visible = false;
            SetState(CursorState.Normal);
        }

        void Update()
        {
            if (cursorRect == null) return;
            cursorRect.position = Input.mousePosition;

            // סומכים רק על הערך שמגיע מהאירוע:
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

        

        private bool isHovering = false;

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


        private CursorState GetCursorState2D()
        {
            // בדיקה אם העכבר מעל UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButton(0))
                {
                    var pointerData = new PointerEventData(EventSystem.current)
                    {
                        position = Input.mousePosition
                    };
                    var results = new System.Collections.Generic.List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointerData, results);

                    foreach (var r in results)
                    {
                        if (r.gameObject.GetComponent<Button>() != null)
                        {
                            return CursorState.Active; // Mouse pressed on UI Button
                        }
                    }
                }
                return CursorState.Normal;
            }

            // בדיקה בעולם
            Vector3 mouseScreen = Input.mousePosition;
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);

            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero, 0f, platformLayerMask);

            if (hit.collider != null)
            {
                var platform = hit.collider.GetComponent<Platform>();
                if (platform != null ) //&& platform.IsReallyActive())
                {
                    if (Input.GetMouseButton(0))
                        return CursorState.Active;
                    else
                        return CursorState.Hover;
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
