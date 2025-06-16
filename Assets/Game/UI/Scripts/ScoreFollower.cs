using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Scripts
{
    public class UIFollowerToWorld : MonoBehaviour
    {
        public RectTransform uiElement;   // The UI element to track
        public Camera mainCamera;         // The camera rendering the world
        public float worldDistance; // Distance from the camera to project the point
        public float followSpeed = 5f; 

        void Update()
        {
            if (uiElement == null || mainCamera == null)
                return;

            // ✅ Step 1: Get screen position of the UI element (NOT its world position!)
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, uiElement.position);

            // ✅ Step 2: Project that screen point into world space at a fixed distance from the camera
            Vector3 screenPoint = new Vector3(screenPos.x, screenPos.y, worldDistance);
            Vector3 targetWorldPoint = mainCamera.ScreenToWorldPoint(screenPoint);

            // ✅ Step 3: Move this object toward the projected world point
            transform.position = Vector3.Lerp(transform.position, targetWorldPoint, followSpeed * Time.deltaTime);
        }
    }
}