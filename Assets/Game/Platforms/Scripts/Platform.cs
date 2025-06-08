using UnityEngine;
using UI;

public class Platform : MonoBehaviour
{
    private bool isHover = false;
    private bool isPressed = false;

    void OnMouseEnter()
    {
        isHover = true;
        UpdateCursor();
    }

    void OnMouseExit()
    {
        isHover = false;
        isPressed = false;
        UpdateCursor();
    }

    void OnMouseDown()
    {
        isPressed = true;
        UpdateCursor();
    }

    void OnMouseUp()
    {
        isPressed = false;
        UpdateCursor();
    }

    private void UpdateCursor()
    {
        var cursor = FindObjectOfType<UICursor>();
        if (cursor == null) return;

        if (isHover)
        {
            if (isPressed)
                cursor.SetState(UICursor.CursorState.Active); // Mouse is pressed on this platform
            else
                cursor.SetState(UICursor.CursorState.Hover);  // Mouse is hovering over this platform
        }
        else
        {
            cursor.SetState(UICursor.CursorState.Normal);      // Mouse is not over this platform
        }
    }
}