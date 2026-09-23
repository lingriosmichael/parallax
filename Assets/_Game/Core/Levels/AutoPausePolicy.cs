namespace Parallax.Core
{
    public enum AppLifecycleEvent { Background, FocusLost }

    /// <summary>PAX-054 (D-073): when the app's lifecycle pauses a level. Going to the background
    /// always pauses. Losing focus pauses unless focus loss is ignored, which LevelPause sets in
    /// the Editor, where clicking any other window loses focus. Coming back never resumes: that
    /// is the player's tap on Resume.</summary>
    public static class AutoPausePolicy
    {
        public static bool ShouldPause(AppLifecycleEvent lifecycleEvent, bool ignoreFocusLoss) =>
            lifecycleEvent == AppLifecycleEvent.Background || !ignoreFocusLoss;
    }
}
