namespace DeathNotice.Core
{
    /// <summary>
    /// Placeholder for the pure-logic layer. Code in DeathNotice.Core must not
    /// depend on scenes, GameObjects or MonoBehaviours so it stays unit-testable.
    /// </summary>
    public static class CoreInfo
    {
        public const string GameName = "Death Notice";

        public static int Add(int a, int b)
        {
            return a + b;
        }
    }
}
