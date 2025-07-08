using System;
using Game.Core.Score;
using UnityEngine;
using System.Collections;
using Game.Core.Audio;
using Game.Core.Camera.Scripts;
using Game.Core.Generics;
using Game.Core.Utils;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Game.Core.Managers
{
    public class GameManager : MonoSingleton<GameManager>
    {
        private GameplayScore _gameplayScore;
        private HighScoreManager _highScoreManager;
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
            GameEvents.OnGameStarted += GameSarted;
            GameEvents.OnTutorialStarted += TutorialStarted;
        }

        private void TutorialStarted()
        {
            GameEvents.OnGameFinished += OnTutorialReset;
        }

        private void OnTutorialReset()
        {
            GameEvents.OnGameFinished -= OnTutorialReset;
            SceneLoader.Instance.TriggerClose(() =>
                SceneLoader.Instance.LoadSceneWithCallback(1, () =>
                    GameEvents.TutorialReset((Action action) =>
                        SceneLoader.Instance.TriggerOpen(action)
                    )
                )
            );
        }

        private void GameSarted()
        {
            GameEvents.OnGameFinished += OnGameFinished;
        }

        public void OnDisable()
        {
            GameEvents.OnGameInitialization -= InitializeRelevantObjects;
            GameEvents.OnGameFinished -= OnGameFinished;
            GameEvents.OnTutorialStarted -= TutorialStarted;
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
            FirebaseBridge.Instance.Initialize();
            if (_highScoreManager == null) _highScoreManager = new HighScoreManager(highScoreLogger);
            if (_gameplayScore == null) _gameplayScore = new GameplayScore();
            _timeStarted = Time.time;
            if (CurrentNickname == null)
            {
                int idx = Random.Range(0, _randomNames.Length);
                CurrentNickname = _randomNames[idx];
            }

            UnityMainThreadDispatcher.Instance.StartObject();
        }

        private void OnGameFinished()
        {
            GameEvents.OnGameFinished -= OnGameFinished;
            float finishedTime = Time.time - _timeStarted;
            _highScoreManager.TryAddHighScore(CurrentNickname,_gameplayScore.Score,  finishedTime);
            var camera = GameObject.FindFirstObjectByType<HybridCameraFollow>();
            camera.tutorialModeTargetFrame = 6;
            AudioManager.Instance.Play(AudioName.CurtainGameToEnd,Vector3.zero);
            camera.AdjustTargetFraming(() =>
                SceneLoader.Instance.TriggerClose(() =>
                    SceneLoader.Instance.SetSkeletonSortingLayer("Curtain",() =>
                    SceneLoader.Instance.LoadSceneWithCallback(3, () =>
                    {
                        AudioManager.Instance.Play(AudioName.EndMusic, Vector3.zero);
                        SceneLoader.Instance.TriggerOut(() => SceneLoader.Instance.SetSkeletonSortingLayer("default"));
                    }))));
        }


        public void StartGame()
        {
            AudioManager.Instance.Play(AudioName.CurtainOpenToGame,Vector3.zero);
            SceneLoader.Instance.SetSkeletonSortingLayer("Curtain", () =>
                SceneLoader.Instance.TriggerClose(() =>
                    SceneLoader.Instance.LoadSceneWithCallback(2, () =>
                        SceneLoader.Instance.SetSkeletonSortingLayer("default", ()
                            => SceneLoader.Instance.TriggerOpen(() =>
                                StartCoroutine(GameStartCamera()))))));
        }

        public void StartGameFromTutorial()
        {
            SceneLoader.Instance.SetSkeletonSortingLayer("Curtain", () =>
                    SceneLoader.Instance.TriggerClose(() =>
                        SceneLoader.Instance.LoadSceneWithCallback(1, 
                            () => SceneLoader.Instance.SetSkeletonSortingLayer("default", () =>
                            SceneLoader.Instance.TriggerOpen(
                                 GameEvents.TutorialStarted )))));
        }

        private IEnumerator GameStartCamera()
        {
            yield return null;
            var camera = FindFirstObjectByType<HybridCameraFollow>();
            camera.tutorialModeTargetFrame = 0;
            camera.AdjustTargetFraming(GameEvents.GameStarted);
        }

        public void BackToStartScreen()
        {
            AudioManager.Instance.Play(AudioName.CurtainEndToOpening,Vector3.zero);
            SceneLoader.Instance.SetSkeletonSortingLayer("Curtain", () =>
                SceneLoader.Instance.TriggerClose(() =>
                    SceneLoader.Instance.LoadSceneWithCallback(0, () =>
                        SceneLoader.Instance.TriggerOut(()
                            => SceneLoader.Instance.SetSkeletonSortingLayer("default")))));
        }
    }
}