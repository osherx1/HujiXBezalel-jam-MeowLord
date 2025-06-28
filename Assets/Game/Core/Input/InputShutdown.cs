using Game.Core.Generics;
using UnityEngine;

namespace Game.Core.Input
{
    public class InputShutdown : MonoSingleton<MonoBehaviour>
    {
        void OnApplicationQuit()
        {
            InputSystemSingleton.Instance.InputSystem.Disable();
        }
    }
}