using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace Game.Core.Tutorial
{
    public class TutorialTextRenderer: MonoBehaviour
    {
        [Header("Tutorial UI Elements")]
        [SerializeField] private TMP_Text tutorialText;
        [SerializeField] private GameObject textGameObject;
        [SerializeField] private Image blurPanel;
        [SerializeField, Range(0f, 1f)] private float blurAlpha = 0.7f;
        [SerializeField] private float blurDuration = 0.5f;
        [SerializeField] private float textFadeDuration = 0.5f;

        private Coroutine blurCoroutine;
        private Coroutine textCoroutine;

        private void SetPanelAlpha(float alpha)
        {
            if (blurPanel != null)
            {
                var color = blurPanel.color;
                color.a = alpha;
                blurPanel.color = color;
            }
        }

        public void ShowBlurAndText(string message)
        {
            ShowBlurAndText(message, null, null);
        }

        public void ShowBlurAndText(string message, Vector3? worldPosition)
        {
            ShowBlurAndText(message, worldPosition, null);
        }

        public void ShowBlurAndText(string message, System.Action onComplete)
        {
            ShowBlurAndText(message, null, onComplete);
        }
        public void ShowBlurAndText(string message, Vector3? worldPosition, System.Action onComplete)
        {
            if (blurCoroutine != null) StopCoroutine(blurCoroutine);
            if (textCoroutine != null) StopCoroutine(textCoroutine);
            blurCoroutine = StartCoroutine(BlurInCoroutine(blurAlpha, blurDuration, () => {
                if (worldPosition.HasValue)
                {
                    // Convert world position to screen position for UI
                    Vector3 screenPos = UnityEngine.Camera.main.WorldToScreenPoint(worldPosition.Value);
                    textGameObject.transform.position = screenPos;
                }
                textCoroutine = StartCoroutine(ShowTextCoroutine(message, textFadeDuration, onComplete));
            }));
        }

        public void HideBlurAndText()
        {
            if (blurCoroutine != null) StopCoroutine(blurCoroutine);
            if (textCoroutine != null) StopCoroutine(textCoroutine);
            StartCoroutine(BlurOutAndHideTextCoroutine());
        }

        public void TransitionText(string newMessage, System.Action onComplete = null)
        {
            if (textCoroutine != null) StopCoroutine(textCoroutine);
            textCoroutine = StartCoroutine(TransitionTextCoroutine(newMessage, textFadeDuration, onComplete));
        }

        private IEnumerator BlurInCoroutine(float targetAlpha, float duration, System.Action onComplete)
        {
            blurPanel.gameObject.SetActive(true);
            float startAlpha = blurPanel.color.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float lerpAlpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
                SetPanelAlpha(lerpAlpha);
                yield return null;
            }
            SetPanelAlpha(targetAlpha);
            onComplete?.Invoke();
        }

        private IEnumerator ShowTextCoroutine(string message, float duration, System.Action onComplete = null)
        {
            tutorialText.text = message;
            textGameObject.SetActive(true);
            Color c = tutorialText.color;
            float startAlpha = 0f;
            float targetAlpha = 1f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float lerpAlpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
                c.a = lerpAlpha;
                tutorialText.color = c;
                yield return null;
            }
            c.a = targetAlpha;
            tutorialText.color = c;
            onComplete?.Invoke();
        }

        private IEnumerator BlurOutAndHideTextCoroutine()
        {
            // Fade out text
            Color c = tutorialText.color;
            float startAlpha = c.a;
            float t = 0f;
            while (t < textFadeDuration)
            {
                t += Time.deltaTime;
                float lerpAlpha = Mathf.Lerp(startAlpha, 0f, t / textFadeDuration);
                c.a = lerpAlpha;
                tutorialText.color = c;
                yield return null;
            }
            c.a = 0f;
            tutorialText.color = c;
            textGameObject.SetActive(false);

            // Fade out blur
            float panelStartAlpha = blurPanel.color.a;
            t = 0f;
            while (t < blurDuration)
            {
                t += Time.deltaTime;
                float lerpAlpha = Mathf.Lerp(panelStartAlpha, 0f, t / blurDuration);
                SetPanelAlpha(lerpAlpha);
                yield return null;
            }
            SetPanelAlpha(0f);
            blurPanel.gameObject.SetActive(false);
        }

        private IEnumerator TransitionTextCoroutine(string newMessage, float duration, System.Action onComplete = null)
        {
            // Fade out current text
            Color c = tutorialText.color;
            float startAlpha = c.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float lerpAlpha = Mathf.Lerp(startAlpha, 0f, t / duration);
                c.a = lerpAlpha;
                tutorialText.color = c;
                yield return null;
            }
            c.a = 0f;
            tutorialText.color = c;

            // Change text
            tutorialText.text = newMessage;

            // Fade in new text
            t = 0f;
            float targetAlpha = 1f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float lerpAlpha = Mathf.Lerp(0f, targetAlpha, t / duration);
                c.a = lerpAlpha;
                tutorialText.color = c;
                yield return null;
            }
            c.a = targetAlpha;
            tutorialText.color = c;
            onComplete?.Invoke();
        }
    }
}