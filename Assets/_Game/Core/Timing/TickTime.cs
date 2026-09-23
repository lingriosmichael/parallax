using System;
using UnityEngine;

namespace Parallax.Core
{
    /// <summary>PAX-077 (D-075): the one seam every seconds&lt;-&gt;ticks conversion goes through. A
    /// tick is one FixedUpdate (ObserverSet.FixedUpdate owns the tick count), so the source is
    /// Time.fixedDeltaTime: 0.02 s, 50 Hz, pinned by TickTimeTests. Conversions are floats except ToWholeTicks (half up, D-077);
    /// the stored step is Unity 6's rational 2822399/141120000 s, a hair under 0.02, so a caller
    /// that ceils a whole-second value (0.6 s / step = 30.00001) would gain a tick.
    /// SecondsPerTickSource is public so tests can swap the rate without writing the real
    /// Time.fixedDeltaTime; production code never reassigns it.</summary>
    public static class TickTime
    {
        public static Func<float> SecondsPerTickSource = Default;

        static float Default() => Time.fixedDeltaTime;

        // Same safety net as RunningState: a test that dies before its teardown can't leave a fake
        // installed into a later Play mode session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetToDefault() => SecondsPerTickSource = Default;

        public static float SecondsPerTick => SecondsPerTickSource();
        public static float TicksPerSecond => 1f / SecondsPerTick;
        public static float ToTicks(float seconds) => seconds / SecondsPerTick;
        public static float ToSeconds(float ticks) => ticks * SecondsPerTick;

        /// <summary>PAX-079 (D-077): a seconds value as a whole tick count, rounded half up, so a
        /// whole hundredth of a second gives the same count at 0.02f and at the stored step
        /// (RoundToInt's half-to-even would give 0.05 s = 2 at 0.02f and 3 at the stored step).</summary>
        public static int ToWholeTicks(float seconds) => Mathf.FloorToInt(seconds / SecondsPerTick + 0.5f);
    }
}
