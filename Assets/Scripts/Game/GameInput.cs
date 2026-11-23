using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>
    /// Where the game reads keyboard / mouse state from. The default reads UnityEngine.Input (legacy
    /// Input Manager); automated playtests swap in a fake via <see cref="GameInput.Source"/>.
    /// Player behaviour is identical: Shift is still a pure hold check.
    /// </summary>
    public interface IGameInput
    {
        bool GetKey(KeyCode key);
        bool GetKeyDown(KeyCode key);
        bool GetMouseButton(int button);
        bool GetMouseButtonDown(int button);
        Vector3 MousePosition { get; }
    }

    public static class GameInput
    {
        private class UnityInput : IGameInput
        {
            public bool GetKey(KeyCode key) { return Input.GetKey(key); }
            public bool GetKeyDown(KeyCode key) { return Input.GetKeyDown(key); }
            public bool GetMouseButton(int button) { return Input.GetMouseButton(button); }
            public bool GetMouseButtonDown(int button) { return Input.GetMouseButtonDown(button); }
            public Vector3 MousePosition { get { return Input.mousePosition; } }
        }

        private static readonly IGameInput Default = new UnityInput();
        private static IGameInput source;

        /// <summary>Set to a fake for tests; null restores the real input.</summary>
        public static IGameInput Source
        {
            get { return source ?? Default; }
            set { source = value; }
        }

        public static bool GetKey(KeyCode key) { return Source.GetKey(key); }
        public static bool GetKeyDown(KeyCode key) { return Source.GetKeyDown(key); }
        public static bool GetMouseButton(int button) { return Source.GetMouseButton(button); }
        public static bool GetMouseButtonDown(int button) { return Source.GetMouseButtonDown(button); }
        public static Vector3 MousePosition { get { return Source.MousePosition; } }

        public static bool ShiftHeld
        {
            get { return GetKey(KeyCode.LeftShift) || GetKey(KeyCode.RightShift); }
        }
    }
}
