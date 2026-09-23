using System;
using UnityEngine;

namespace Parallax.Gameplay.Levels
{
    /// <summary>PAX-054 (D-073): the one seam every write to Time.timeScale goes through
    /// (LevelPause and LevelSceneLoader). Time scale 0 is the global stop while a level is
    /// paused: physics, every FixedUpdate (including frozen co-op code) and deltaTime-driven
    /// presentation stop with it. SetTimeScale is public so tests can record writes instead of
    /// touching the real Time.timeScale; production code never reassigns it.</summary>
    public static class RunningState
    {
        public const float RunningTimeScale = 1f;
        public const float PausedTimeScale = 0f;

        public static Action<float> SetTimeScale = DefaultSetTimeScale;

        static void DefaultSetTimeScale(float value) => Time.timeScale = value;

        // Same safety net as LevelSceneLoader: a test that dies before its teardown can't leave a
        // fake installed into a later Play mode session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetToProductionDefaults() => SetTimeScale = DefaultSetTimeScale;

        public static void Freeze() => SetTimeScale(PausedTimeScale);
        public static void Restore() => SetTimeScale(RunningTimeScale);
    }
}
