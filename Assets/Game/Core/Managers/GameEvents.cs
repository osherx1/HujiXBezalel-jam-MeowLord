using System;

namespace Game.Core.Managers
{
    public static class GameEvents
    {
        public static event Action OnPlayerMoved;
        
        public static void PlayerMoved()
        {
            OnPlayerMoved?.Invoke();
        }
    }
}