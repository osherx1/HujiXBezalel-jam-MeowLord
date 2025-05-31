using UnityEngine;
using TMPro;
using Game.Core.Managers;

namespace Game.UI.Scripts
{
    public class NumberOfSegmentsRenderer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI segmentsText;

        private void OnEnable()
        {
            GameEvents.OnNumberOfSegmentsChanged += UpdateSegmentsText;
        }

        private void OnDisable()
        {
            GameEvents.OnNumberOfSegmentsChanged -= UpdateSegmentsText;
        }

        private void UpdateSegmentsText(int value)
        {
            if (segmentsText != null)
                segmentsText.text = $"Lines: {value}";
        }
    }
} 