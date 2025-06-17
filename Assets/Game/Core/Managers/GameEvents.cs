using System;
using Game.Platforms.Scripts;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Core.Managers
{
    public static class GameEvents
    {
        public static event Action OnGameInitialization;
        public static event Action OnPlayerFall;
        public static event Action OnPlayerMoved;
        public static event Action OnGameFinished;

        public static event Action<Vector3> OnMouseCatch;
        
        public static event Action<bool> OnAfraidChanged;

        public static event Action<int> OnUpdateScore;

        public static event Action OnGameStarted;

        public static event Action<int> OnNumberOfSegmentsChanged;
        
        public static event Action OnPlayerLanded;
        

        public static event Action<bool> OnPlatformHover;
        public static event Action OnGamePause;

        public static void PlatformHover(bool isHovering)
        {
            OnPlatformHover?.Invoke(isHovering);
        }

        public static void PlayerLanded()
        {
            OnPlayerLanded?.Invoke();
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

        public static void GameInitialization()
        {
            OnGameInitialization?.Invoke();
        }

        public static event Action OnScoreCombinatorReady;

        public static void ScoreCombinatorReady()
        {
            OnScoreCombinatorReady?.Invoke();
        }

        public static void GamePause()
        {
            OnGamePause?.Invoke();
        }

        public static event Action OnGameResume;

        public static void GameResume()
        {
            OnGameResume?.Invoke();
        }

        public static event Action OnPlayerPause;

        public static void PlayerPause()
        {
            OnPlayerPause?.Invoke();
        }

        public static event Action OnPlayerResume;

        public static void PlayerResume()
        {
            OnPlayerResume?.Invoke();
        }

        public static event Action<PlatformType> OnSpawnPlatform;

        public static void SpawnPlatform(PlatformType obj)
        {
            OnSpawnPlatform?.Invoke(obj);
        }
        
        
    }
}