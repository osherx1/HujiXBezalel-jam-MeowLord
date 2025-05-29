using Game.Core.Score;
using UnityEngine;
using System.Collections;
using Game.Core.Generics;
using Game.Core.Utils;
using UnityEngine.SceneManagement;

namespace Game.Core.Managers
{
    public class GameManager: MonoSingleton<GameManager>
    {
        private  GameplayScore _gameplayScore;
        private  HighScoreManager _highScoreManager;
        private FirebaseInitializer _firebaseIntializer;
        [SerializeField] private HighScoreLogger highScoreLogger;

        public GameplayScore GameplayScore => _gameplayScore;
        public HighScoreManager HighScoreManager => _highScoreManager;

        // The single instance, lazily initialized
        
        private static readonly string[] _randomNames = new string[]
        {
            "Pixel", "Whiskers", "Shadow", "Nova", "Milo",
            "Luna", "Ziggy", "Pepper", "Mochi", "Ninja",
            "Jinx", "Maple", "Muffin", "Rocket", "Hazel",
            "Blitz", "Mocha", "Sprout", "Olive", "Cosmo"
        };

        public string CurrentNickname { get; private set; }
        

        // Private constructor prevents external instantiation
        public void Awake()
        {
            _firebaseIntializer = new FirebaseInitializer(highScoreLogger);
            _highScoreManager = new HighScoreManager(highScoreLogger);
            int idx = Random.Range(0, _randomNames.Length);
            CurrentNickname = _randomNames[idx];
            _gameplayScore = new GameplayScore();
            GameEvents.OnGameFinished += OnGameFinished;
            GameEvents.OnGameStarted += StartGameFinishedTimer;
            GameEvents.GameStarted();
        }

        private void OnGameFinished()
        {
            _highScoreManager.TryAddHighScore(_gameplayScore.Score, CurrentNickname);
        }

        private void StartGameFinishedTimer()
        {
            StartCoroutine(GameFinishedTimerCoroutine());
        }

        private IEnumerator GameFinishedTimerCoroutine()
        {
            yield return new WaitForSeconds(60f);
            GameEvents.GameFinished();
            SceneManager.LoadScene("EndScene");
            yield return null;
            GameEvents.EndSceneStarted();
        }
    }
}