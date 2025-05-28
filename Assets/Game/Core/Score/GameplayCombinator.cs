using System.Collections.Generic;
using Game.Core.Managers;
using UnityEngine;

namespace Game.Core.Score
{
    public class GameplayCombinator
    {
        private enum EventType
        {
            PlayerFall,
            MouseCatch
        }

        private readonly GameplayScore _gameplayScore;
        private readonly List<EventType> _eventsThisFrame = new();
        private GameObject _updaterGO;
        private GameplayCombinatorUpdater _updater;

        public GameplayCombinator(GameplayScore gameplayScore)
        {
            _gameplayScore = gameplayScore;
            GameEvents.OnGameStarted += CreateGameplayCombinatorUpdater;
            GameEvents.OnGameFinished += DestroyGameplayCombinatorUpdater;
        }

        private void CreateGameplayCombinatorUpdater()
        {
            // Subscribe to events
            GameEvents.OnPlayerFall += OnPlayerFall;
            GameEvents.OnMouseCatch += OnMouseCatch;

            if (_updaterGO == null)
            {
                _updaterGO = new GameObject("GameplayCombinatorUpdater");
                _updater = _updaterGO.AddComponent<GameplayCombinatorUpdater>();
                _updater.Init(this);
            }
        }

        private void DestroyGameplayCombinatorUpdater()
        {
            // Unsubscribe from events
            GameEvents.OnPlayerFall -= OnPlayerFall;
            GameEvents.OnMouseCatch -= OnMouseCatch;

            if (_updaterGO != null)
            {
                Object.Destroy(_updaterGO);
                _updaterGO = null;
                _updater = null;
            }
        }

        private void OnPlayerFall()
        {
            _eventsThisFrame.Add(EventType.PlayerFall);
        }

        private void OnMouseCatch()
        {
            _eventsThisFrame.Add(EventType.MouseCatch);
        }

        // Should be called once per frame (e.g., from a MonoBehaviour proxy)
        public void LateUpdate()
        {
            int mouseCatchCount = 0;
            int playerFallCount = 0;
            foreach (var evt in _eventsThisFrame)
            {
                if (evt == EventType.MouseCatch) mouseCatchCount++;
                if (evt == EventType.PlayerFall) playerFallCount++;
            }

            // Example scoring logic
            if (mouseCatchCount == 1)
                _gameplayScore.AddScore(100);
            else if (mouseCatchCount == 2)
                _gameplayScore.AddScore(300);
            else if (mouseCatchCount > 2)
                _gameplayScore.AddScore(300 + (mouseCatchCount - 2) * 100); // e.g., 400 for 3, 500 for 4, etc.

            if (playerFallCount > 0)
                _gameplayScore.AddScore(-300 * playerFallCount);

            _eventsThisFrame.Clear();
        }

        // MonoBehaviour proxy for calling LateUpdate
        private class GameplayCombinatorUpdater : MonoBehaviour
        {
            private GameplayCombinator _combinator;
            public void Init(GameplayCombinator combinator)
            {
                _combinator = combinator;
            }
            private void LateUpdate()
            {
                _combinator?.LateUpdate();
            }
        }
    }
} 