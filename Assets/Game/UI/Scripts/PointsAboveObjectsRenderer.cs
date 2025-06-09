using Game.Core.Score;
using UnityEngine;
using TMPro;

namespace Game.UI.Scripts
{
    public class PointsAboveObjectsRenderer: MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI pointsTextPrefab;
        [SerializeField] private Transform worldSpaceCanvasTransform;
        [SerializeField] [Range(0.1F,5F)] private float timeUntilDispawn;

        private void OnEnable()
        {
            GameplayScoreCombinator.RenderPoints += RenderPoints;
        }

        private void OnDisable()
        {
            GameplayScoreCombinator.RenderPoints  -= RenderPoints;
        }

        private void RenderPoints(Vector3 worldPosition, int value)
        {
            if (pointsTextPrefab == null || worldSpaceCanvasTransform == null) return;
            var textObj = Instantiate(pointsTextPrefab, worldSpaceCanvasTransform);
            
            Vector3 localPos = worldSpaceCanvasTransform.InverseTransformPoint(worldPosition);
            textObj.rectTransform.localPosition = localPos;

            textObj.text = value.ToString();
            Destroy(textObj.gameObject, timeUntilDispawn);
        }
    }
}