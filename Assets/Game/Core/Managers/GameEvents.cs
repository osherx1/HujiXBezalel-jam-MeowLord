using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Core.Managers
{
    public static class GameEvents
    {
        public static event Action OnPlayerFall;
        public static event Action OnPlayerMoved;
        public static event Action OnGameFinished;

        public static event Action<Vector3> OnMouseCatch;
        
        public static event Action<bool> OnAfraidChanged;

        public static event Action<int> OnUpdateScore;

        public static event Action OnGameStarted;

        public static event Action<int> OnNumberOfSegmentsChanged;
        
        public static event Action OnPlayerLanded;

        public static event Action<MonoBehaviour> OnPlayerMovingPlatform;

        public static void PlayerLanded()
        {
            OnPlayerLanded?.Invoke();
        }

        public static void PlayerMovingPlatform(MonoBehaviour courtineProxy)
        {
            OnPlayerMovingPlatform?.Invoke(courtineProxy);
        }

        public static void GameStarted()
        {
            OnGameStarted?.Invoke();
        }

        public static void UpdateScore(int score)
        {
            OnUpdateScore?.Invoke(score);
        }
        
        public static void AfraidChanged(bool value)
        {
            OnAfraidChanged?.Invoke(value);
        }
        public static void PlayerMoved()
        {
            OnPlayerMoved?.Invoke();
        }


        public static void GameFinished()
        {
            OnGameFinished?.Invoke();
        }
        
        public static void MouseCatch(Vector3 mousePosition)
        {
            OnMouseCatch?.Invoke(mousePosition);
        }


        public static void PlayerFall()
        {
            OnPlayerFall?.Invoke();
        }

        public static event Action OnEndSceneStarted;

        public static void EndSceneStarted()
        {
            OnEndSceneStarted?.Invoke();
        }

        public static void NumberOfSegmentsChanged(int value)
        {
            OnNumberOfSegmentsChanged?.Invoke(value);
        }

        public static event Action<Vector3> OnPlayerFallPointsUpdate;

        public static void PlayerFallPointsUpdate(Vector3 playerPosition)
        {
            OnPlayerFallPointsUpdate?.Invoke(playerPosition);
        }
    }
}