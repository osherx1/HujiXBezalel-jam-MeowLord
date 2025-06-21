using System.Collections;
using DG.Tweening;
using Game.Core.Managers;
using Game.Core.Score;
using TMPro;
using UnityEngine;

namespace Game.UI.Scripts
{
    public class GameplayScoreRenderer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private GameObject yarn;

        private GameplayScore _gameplayScore;
        private Coroutine courtine;

        private void Start()
        {
            if(yarn != null) yarn.SetActive(false);
            // Subscribe to global score update event
            GameEvents.OnUpdateScore += UpdateScoreDisplay;
            GameEvents.OnYarnAdded += DisplayYarn;
            // Set initial value
            UpdateScoreDisplay(0);
        }

        private void DisplayYarn()
        {
            if(yarn == null || courtine != null) return;
            courtine = StartCoroutine(DisplayYarnCourtine());
        }

        private IEnumerator DisplayYarnCourtine()
        {
            yarn.SetActive(true);
            
            // Make the score text blink. 
            // The tween has an even number of loops, so it will end at the original alpha.
            scoreText.DOFade(0.2f, 0.25f).SetLoops(8, LoopType.Yoyo);
            
            yield return yarn.transform.DOPunchScale(Vector3.one * 0.5f, 0.5f);
            yield return new WaitForSeconds(2f);
            yarn.SetActive(false);
            courtine = null;
        }

        private void OnDestroy()
        {
            GameEvents.OnUpdateScore -= UpdateScoreDisplay;
            GameEvents.OnYarnAdded -= DisplayYarn;
        }

        private void UpdateScoreDisplay(int score)
        {
            if (scoreText != null)
                scoreText.text = $"{score}";
        }
    }
}