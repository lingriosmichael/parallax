namespace Parallax.Core
{
    public enum PulseGatePhase { Off, Warn, On }

    /// <summary>PAX-112 (D-176): the pulse gate as pure functions of ticks. Its cycle is Periodic (D-055 (2)): it fires every
    /// period from room tick `phase`; the fire tick is the warning's first tick, then warn (harmless) -> on (lethal) -> off.</summary>
    public static class PulseGateMath
    {
        public const int MinWarnTicks = 6;

        /// <summary>The phase `ticksSinceFire` ticks after the latest fire; negative = never fired.</summary>
        public static PulseGatePhase PhaseSince(int ticksSinceFire, int warnTicks, int onTicks)
        {
            if (ticksSinceFire < 0) return PulseGatePhase.Off;
            if (ticksSinceFire < warnTicks) return PulseGatePhase.Warn;
            return ticksSinceFire < warnTicks + onTicks ? PulseGatePhase.On : PulseGatePhase.Off;
        }

        /// <summary>The phase at a room tick, fired every `periodTicks` from `phaseTicks` (TrapTiming's Periodic rule).</summary>
        public static PulseGatePhase PhaseAt(int roomTick, int periodTicks, int phaseTicks, int warnTicks, int onTicks)
        {
            if (roomTick < phaseTicks || periodTicks < 1) return PulseGatePhase.Off;
            return PhaseSince((roomTick - phaseTicks) % periodTicks, warnTicks, onTicks);
        }

        /// <summary>The ticks the gate is off in each period (none when warn + on fill it).</summary>
        public static int OffTicks(int periodTicks, int warnTicks, int onTicks) => System.Math.Max(0, periodTicks - warnTicks - onTicks);

        /// <summary>The warning flickers: lit on even ticks of the warning, dim on odd ones.</summary>
        public static bool WarnLit(int ticksSinceFire) => ticksSinceFire >= 0 && ticksSinceFire % 2 == 0;
    }
}
