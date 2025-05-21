using System;

namespace Game.Core.Managers
{
    public static class GameEvents
    {
        public static event Action OnPlayerMoved;
        public static event Action OnTimerFinished;
        public static event Action OnTimerStarted;

        public static event Action OnMouseCatch;
        public static void PlayerMoved()
        {
            OnPlayerMoved?.Invoke();
        }


        public static void TimerFinished()
        {
            OnTimerFinished?.Invoke();
        }

        public static void TimerStarted()
        {
            OnTimerStarted?.Invoke();
        }

        public static void MouseCatch()
        {
            OnMouseCatch?.Invoke();
        }
        
        
    }
}