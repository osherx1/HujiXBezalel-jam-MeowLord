using System;

namespace Game.Core.Managers
{
    public static class GameEvents
    {
        public static event Action OnPlayerFall;
        public static event Action OnPlayerMoved;
        public static event Action OnGameFinished;

        public static event Action OnMouseCatch;
        
        public static event Action<bool> OnAfraidChanged;

        public static event Action<int> OnUpdateScore;

        public static event Action OnGameStarted;

        public static event Action<int> OnNumberOfSegmentsChanged;

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
        
        public static void MouseCatch()
        {
            OnMouseCatch?.Invoke();
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
    }
}