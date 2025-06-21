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

        private void Start()
        {
            yarn.SetActive(false);
            // Subscribe to global score update event
            GameEvents.OnUpdateScore += UpdateScoreDisplay;
            GameEvents.OnYarnAdded += DisplayYarn;
            // Set initial value
            UpdateScoreDisplay(0);
        }

        private void DisplayYarn()
        {
            if(yarn == null) return;
            StartCoroutine(DisplayYarnCourtine());
        }

        private IEnumerator DisplayYarnCourtine()
        {
            yarn.SetActive(true);
            yield return transform.DOPunchScale(Vector3.one * 0.5f, 0.5f);
            yield return new WaitForSeconds(2f);
            yarn.SetActive(false);
            
        }

        private void OnDestroy()
        {
            GameEvents.OnUpdateScore -= UpdateScoreDisplay;
        }

        private void UpdateScoreDisplay(int score)
        {
            if (scoreText != null)
                scoreText.text = $"{score}";
        }
    }
}