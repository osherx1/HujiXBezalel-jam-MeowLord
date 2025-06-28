using System;

namespace Game.Core.Input
{
    public sealed class InputSystemSingleton
    {
        // Lazy<T> ensures thread-safety and deferred initialization
        private static readonly Lazy<InputSystemSingleton> _lazyInstance =
            new Lazy<InputSystemSingleton>(() => new InputSystemSingleton());

        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static InputSystemSingleton Instance => _lazyInstance.Value;

        // Private ctor prevents external instantiation
        private InputSystemSingleton()
        {
            InputSystem = new GameInput();
            InputSystem.Enable();
        }
        
        public GameInput InputSystem { get; private set; }
    }
}