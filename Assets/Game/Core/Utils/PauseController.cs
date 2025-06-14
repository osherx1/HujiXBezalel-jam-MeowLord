using Game.Core.Managers;
using UnityEngine;

namespace Game.Core.Utils
{
    public class PauseController 
    {
        private float _prevFixedDelta;

        public PauseController()
        {
            GameEvents.OnGamePause += PauseGame;
            GameEvents.OnGameResume += ResumeGame;
        }
        public void PauseGame()
        {
            Time.timeScale = 0f;
            // don’t set fixedDeltaTime to zero!

            AudioListener.pause = true;
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}