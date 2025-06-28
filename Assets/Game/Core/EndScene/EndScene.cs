using Game.Core.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core.EndScene
{
    public class EndScene: MonoBehaviour
    {
        
        private bool active = true;
        public void OnPlayButtonClick()
        {
            if(!active) return;
            GameManager.Instance.BackToStartScreen();
            active = false;
        }
    }
}