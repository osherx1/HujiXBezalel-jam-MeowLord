using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Scripts
{
    public class UIFollowerToWorld : MonoBehaviour
    {
        public RectTransform uiElement;
        public Camera mainCamera;
        public float worldDistance;
        public float followSpeed = 5f;

        void Update()
        {
            if (uiElement == null || mainCamera == null)
                return;

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, uiElement.position);

            Vector3 screenPoint = new Vector3(screenPos.x, screenPos.y, worldDistance);
            Vector3 targetWorldPoint = mainCamera.ScreenToWorldPoint(screenPoint);

            transform.position = Vector3.Lerp(transform.position, targetWorldPoint, followSpeed * Time.deltaTime);
        }
    }
}