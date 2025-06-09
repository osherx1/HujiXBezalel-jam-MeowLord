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
        private float _timeStarted;

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
        
        public void SetNickname(string nickname)
        {
            if (!string.IsNullOrWhiteSpace(nickname))
                CurrentNickname = nickname;
        }

        

        // Private constructor prevents external instantiation
        public void Awake()
        {
            _firebaseIntializer = new FirebaseInitializer(highScoreLogger);
            _highScoreManager = new HighScoreManager(highScoreLogger);
            int idx = Random.Range(0, _randomNames.Length);
            CurrentNickname = _randomNames[idx];
            _gameplayScore = new GameplayScore();
            _timeStarted = Time.time;
            GameEvents.OnGameFinished += OnGameFinished;
            GameEvents.GameStarted();
            UnityMainThreadDispatcher.Instance.StartObject();
        }

        private void OnGameFinished()
        {
            float finishedTime = Time.time - _timeStarted;
            _highScoreManager.TryAddHighScore(_gameplayScore.Score, CurrentNickname, finishedTime);
            StartCoroutine(GameFinishedTimerCoroutine());
        }

        
        private IEnumerator GameFinishedTimerCoroutine()
        {
            SceneManager.LoadScene("end");
            yield return null;
            GameEvents.EndSceneStarted();
        }
    }
}