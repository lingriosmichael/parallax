using UnityEngine;

namespace Parallax.Core
{
    public enum GeyserDirection { Up, Down }
    public enum GeyserPhase { Idle, Tell, Erupt }

    /// <summary>PAX-086 (D-088): the geyser as pure functions of ticks. The cycle is Periodic (D-055 (2)): it fires every
    /// period from room tick `phase`; the fire tick is the tell's first tick, then tell -> erupt -> idle. While erupting,
    /// the cat's velocity along the push is set (absolute) to the launch speed; the cross component is kept. The flight
    /// numbers are discrete, as the motor and physics step them: the motor adds g x tick before each move.</summary>
    public static class GeyserMath
    {
        public const int DefaultTellTicks = 25, DefaultEruptTicks = 40, MinTellTicks = 6;
        public const float DefaultColumnWidth = 1f, DefaultColumnHeight = 1.5f, DefaultLaunchSpeed = 14f;

        public static Vector2 Push(GeyserDirection direction) => direction == GeyserDirection.Up ? Vector2.up : Vector2.down;

        /// <summary>The phase `ticksSinceFire` ticks after the latest fire; negative = never fired.</summary>
        public static GeyserPhase PhaseSince(int ticksSinceFire, int tellTicks, int eruptTicks)
        {
            if (ticksSinceFire < 0) return GeyserPhase.Idle;
            if (ticksSinceFire < tellTicks) return GeyserPhase.Tell;
            return ticksSinceFire < tellTicks + eruptTicks ? GeyserPhase.Erupt : GeyserPhase.Idle;
        }

        /// <summary>The phase at a room tick, fired every `periodTicks` from `phaseTicks` (TrapTiming's Periodic rule).</summary>
        public static GeyserPhase PhaseAt(int roomTick, int periodTicks, int phaseTicks, int tellTicks, int eruptTicks)
        {
            if (roomTick < phaseTicks || periodTicks < 1) return GeyserPhase.Idle;
            return PhaseSince((roomTick - phaseTicks) % periodTicks, tellTicks, eruptTicks);
        }

        /// <summary>The launch: the component along `direction` (a unit vector) becomes `speed`; the rest is kept.</summary>
        public static Vector2 Launch(Vector2 velocity, Vector2 direction, float speed) =>
            velocity - direction * Vector2.Dot(velocity, direction) + direction * speed;

        /// <summary>Ticks a cat starting at the vent face is pushed: every tick its collider starts inside the column.</summary>
        public static int PushTicks(float columnHeight, float speed, float secondsPerTick)
        {
            float step = speed * secondsPerTick;
            return step <= 0f ? 0 : Mathf.Max(1, Mathf.CeilToInt(columnHeight / step - 1e-4f));
        }

        /// <summary>The rise after the last push: the sum of (speed - k g tick) x tick over the ticks that still rise.</summary>
        public static float Rise(float speed, float gravity, float secondsPerTick, out int ticks)
        {
            ticks = 0;
            float rise = 0f, loss = gravity * secondsPerTick;
            if (loss <= 0f) return 0f;
            for (float v = speed - loss; v > 0f; v -= loss) { rise += v * secondsPerTick; ticks++; }
            return rise;
        }
    }
}
