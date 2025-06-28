using System;
using Game.Core.Generics;

namespace Game.Core.Input
{
    public sealed class InputSystemSingleton: MonoSingleton<InputSystemSingleton>
    {
        protected override void Awake()
        {
            base.Awake();
            InputSystem = new GameInput();
            InputSystem.Enable();
        }
        
        public GameInput InputSystem { get; private set; }
        
        void OnApplicationQuit()
        {
            InputSystem.Disable();
        }
    }
}