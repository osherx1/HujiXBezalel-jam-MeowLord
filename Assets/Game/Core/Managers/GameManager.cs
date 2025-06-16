using System;
using Game.Core.Score;
using UnityEngine;
using System.Collections;
using Game.Core.Audio;
using Game.Core.Generics;
using Game.Core.Utils;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Game.Core.Managers
{
    public class GameManager: MonoSingleton<GameManager>
    {
        private  GameplayScore _gameplayScore;
        private  HighScoreManager _highScoreManager;
        private FirebaseInitializer _firebaseIntializer;
        private PauseController _pauseController;
        [SerializeField] private HighScoreLogger highScoreLogger;
        private float _timeStarted;
        
        public HighScoreManager HighScoreManager => _highScoreManager;
        
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

        public void OnEnable()
        {
            GameEvents.OnGameInitialization += InitializeRelevantObjects;
            GameEvents.OnGameFinished += OnGameFinished;
        }
        
        public void OnDisable()
        {
            GameEvents.OnGameInitialization -= InitializeRelevantObjects;
            GameEvents.OnGameFinished -= OnGameFinished;
        }

        private void InitializeRelevantObjects()
        {
            if (_pauseController == null)
            {
                _pauseController = new PauseController();
            }
            if (highScoreLogger == null)
            {
                highScoreLogger = gameObject.AddComponent<HighScoreLogger>();
            }
            if (_firebaseIntializer == null)
            {
                _firebaseIntializer = new FirebaseInitializer(highScoreLogger);
            }
            if(_highScoreManager == null)  _highScoreManager = new HighScoreManager(highScoreLogger);
            if (_gameplayScore == null) _gameplayScore = new GameplayScore();
            _timeStarted = Time.time;
            if (CurrentNickname == null)
            {
                int idx = Random.Range(0, _randomNames.Length);
                CurrentNickname = _randomNames[idx];
            }
            
            //AudioManager.Instance.Play(AudioName.BackgroundMusic, Vector3.zero);
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