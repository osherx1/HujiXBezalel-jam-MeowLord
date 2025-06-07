using System;
using System.Collections.Generic;
using Game.Core.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Core.Score
{
    public class GameplayScoreCombinator
    {
        private enum EventType
        {
            PlayerFall,
            MouseCatch
        }
        
        public static event Action<Vector3,int> RenderPoints;

        private readonly GameplayScore _gameplayScore;
        private readonly List<(EventType type, Vector3 position)> _eventsThisFrame = new();
        private GameObject _updaterGO;
        private GameplayCombinatorUpdater _updater;

        public GameplayScoreCombinator(GameplayScore gameplayScore)
        {
            _gameplayScore = gameplayScore;
            GameEvents.OnGameStarted += CreateGameplayCombinatorUpdater;
            GameEvents.OnGameFinished += DestroyGameplayCombinatorUpdater;
        }

        private void CreateGameplayCombinatorUpdater()
        {
            // Subscribe to events
            GameEvents.OnPlayerFallPointsUpdate += OnPlayerFall;
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
            GameEvents.OnPlayerFallPointsUpdate -= OnPlayerFall;
            GameEvents.OnMouseCatch -= OnMouseCatch;

            if (_updaterGO != null)
            {
                Object.Destroy(_updaterGO);
                _updaterGO = null;
                _updater = null;
            }
        }

        private void OnPlayerFall(Vector3 position)
        {
            _eventsThisFrame.Add((EventType.PlayerFall, position));
        }

        private void OnMouseCatch(Vector3 position)
        {
            _eventsThisFrame.Add((EventType.MouseCatch, position));
        }

        public void LateUpdate()
        {
            UpdatePlayerScore();
        }

        private void UpdatePlayerScore()
        {
            ApplyPlayerFallScore();
            ApplyMouseCatchScore();
            _eventsThisFrame.Clear();
        }

        private void ApplyPlayerFallScore()
        {
            foreach (var evt in _eventsThisFrame)
            {
                if (evt.type == EventType.PlayerFall)
                {
                    int currentScore = _gameplayScore.Score;
                    int penalty = 300 + (currentScore / 100);
                    _gameplayScore.AddScore(-penalty);
                    RenderPoints?.Invoke(evt.position, -penalty);
                }
            }
        }

        private void ApplyMouseCatchScore()
        {
            // Gather all mouse catch events and their positions
            var mousePositions = new List<Vector3>();
            foreach (var evt in _eventsThisFrame)
            {
                if (evt.type == EventType.MouseCatch)
                    mousePositions.Add(evt.position);
            }
            int mouseCount = mousePositions.Count;
            if (mouseCount == 0) return;

            // Calculate points per mouse
            float basePoints = 100f;
            float multiplier = 1f;
            for (int i = 2; i <= mouseCount; i++)
            {
                multiplier *= 1.1f; // Each additional mouse adds 10% on top of the last
            }
            float pointsPerMouse = basePoints * multiplier;
            int intPointsPerMouse = Mathf.RoundToInt(pointsPerMouse);

            // Add total points
            int totalPoints = intPointsPerMouse * mouseCount;
            _gameplayScore.AddScore(totalPoints);

            // Render points for each mouse
            foreach (var pos in mousePositions)
            {
                RenderPoints?.Invoke(pos, intPointsPerMouse);
            }
        }

        // MonoBehaviour proxy for calling LateUpdate
        private class GameplayCombinatorUpdater : MonoBehaviour
        {
            private GameplayScoreCombinator _scoreCombinator;
            public void Init(GameplayScoreCombinator scoreCombinator)
            {
                _scoreCombinator = scoreCombinator;
            }
            private void LateUpdate()
            {
                _scoreCombinator?.LateUpdate();
            }
        }
    }
} 