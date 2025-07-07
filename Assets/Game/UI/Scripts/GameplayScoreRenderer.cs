using System.Collections;
using DG.Tweening;
using Game.Core.Managers;
using Game.Core.Score;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Scripts
{
    public class GameplayScoreRenderer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI yarnText;
        [SerializeField] private GameObject yarn;
        [SerializeField] private Image[] yarnImages;
        [SerializeField] private Color blinkColor = Color.yellow;

        private GameplayScore _gameplayScore;
        private Coroutine courtine;

        private void Start()
        {
            if (yarn != null) yarn.SetActive(false);
            // Subscribe to global score update event
            GameEvents.OnUpdateScore += UpdateScoreDisplay;
            GameEvents.OnYarnAdded += DisplayYarn;
            // Set initial value
            UpdateScoreDisplay(0);
        }

        private void DisplayYarn()
        {
            if (courtine != null) return;
            courtine = StartCoroutine(DisplayYarnCourtine());
        }

        private IEnumerator DisplayYarnCourtine()
        {
            if(yarn != null) yarn.SetActive(true);

            if (yarnText != null)
            {
                // var originalColor = yarnText.color;
                // yarnText.color = blinkColor;
                // yarnText.DOFade(0.2f, 0.25f).SetLoops(8, LoopType.Yoyo).OnComplete(() =>
                // {
                //     yarnText.color = originalColor;
                // });
                yarnText.DOColor(blinkColor, 0.25f).SetLoops(8, LoopType.Yoyo);
                
            }
            if (yarnImages != null)
            {
                foreach (var yarnImage in yarnImages)
                {
                    // yarnImage.DOFade(0.2f, 0.25f).SetLoops(8, LoopType.Yoyo);
                    yarnImage.DOColor(blinkColor, 0.25f).SetLoops(8, LoopType.Yoyo);
                }
            }
            
            if(yarn != null) yield return yarn.transform.DOPunchScale(Vector3.one * 0.5f, 0.5f);
            yield return new WaitForSeconds(2f);
            if(yarn != null) yarn.SetActive(false);
            courtine = null;
        }

        private void OnDestroy()
        {
            GameEvents.OnUpdateScore -= UpdateScoreDisplay;
            GameEvents.OnYarnAdded -= DisplayYarn;
        }

        private void UpdateScoreDisplay(int score)
        {
            int.TryParse(scoreText.text, out int currentScore);

            if (scoreText != null)
                scoreText.text = $"{score}";
            
            if (score > currentScore)
            {
                // var originalColor = scoreText.color;
                // scoreText.color = blinkColor;
                // scoreText.DOFade(0.2f, 0.25f).SetLoops(8, LoopType.Yoyo).OnComplete(() =>
                // {
                //     scoreText.color = originalColor;
                // });
                scoreText.DOColor(blinkColor, 0.25f).SetLoops(8, LoopType.Yoyo);
            }
        }
    }
}