using Game.Core.Managers;
using Game.Core.Score;
using TMPro;
using UnityEngine;

namespace Game.UI.Scripts
{
    public class GameplayScoreRenderer : MonoBehaviour
    {
        [Header("TextMeshPro References")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI nicknameText;

        private GameplayScore _gameplayScore;

        private void Awake()
        {
            // Get nickname from GameManager
            string currentNickname = GameManager.Instance.CurrentNickname;
            if (nicknameText != null)
                nicknameText.text = currentNickname;

            // Get GameplayScore instance from GameManager

            // Subscribe to changes
            if (_gameplayScore != null)
                _gameplayScore.OnScoreChanged += UpdateScoreDisplay;

            // Set initial value
            UpdateScoreDisplay(_gameplayScore?.Score ?? 0);
        }

        private void OnDestroy()
        {
            if (_gameplayScore != null)
                _gameplayScore.OnScoreChanged -= UpdateScoreDisplay;
        }

        private void UpdateScoreDisplay(int score)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score}";
        }
    }
}