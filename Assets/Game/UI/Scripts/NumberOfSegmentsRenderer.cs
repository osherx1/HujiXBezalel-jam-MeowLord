using UnityEngine;
using TMPro;
using Game.Core.Managers;

namespace Game.UI.Scripts
{
    public class NumberOfSegmentsRenderer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI segmentsText;
        [SerializeField] private Animator segmentsAnimator;

        private void OnEnable()
        {
            GameEvents.OnNumberOfSegmentsChanged += UpdateSegmentsText;
            GameEvents.OnNumberOfSegmentsChanged += UpdateSegmentsAnimator;
        }

        private void OnDisable()
        {
            GameEvents.OnNumberOfSegmentsChanged -= UpdateSegmentsText;
            GameEvents.OnNumberOfSegmentsChanged -= UpdateSegmentsAnimator;
        }

        private void UpdateSegmentsText(int value)
        {
            if (segmentsText != null)
                segmentsText.text = $"Lines: {value}";
        }
        private void UpdateSegmentsAnimator(int value)
        {
            segmentsAnimator.SetInteger("Counter", value);
        }
    }
} 