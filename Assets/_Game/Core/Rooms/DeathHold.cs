namespace Parallax.Core
{
    public enum DeathHoldPhase { Live, Holding, ResetNow }

    /// <summary>PAX-047 D-058: counts the ticks a death freezes the room for, in room-life ticks.</summary>
    public sealed class DeathHold
    {
        readonly int holdTicks;
        int remaining;

        public DeathHold(int holdTicks)
        {
            this.holdTicks = holdTicks < 0 ? 0 : holdTicks;
        }

        public bool IsHolding { get; private set; }

        /// <summary>Called once, at the kill tick. HoldTicks 0 resets on the same tick, with no Step() call.</summary>
        public DeathHoldPhase Begin()
        {
            if (IsHolding) return DeathHoldPhase.Holding;
            remaining = holdTicks;
            if (remaining <= 0) return DeathHoldPhase.ResetNow;
            IsHolding = true;
            return DeathHoldPhase.Holding;
        }

        /// <summary>Called once per tick after Begin(), only while IsHolding.</summary>
        public DeathHoldPhase Step()
        {
            if (!IsHolding) return DeathHoldPhase.Live;
            remaining--;
            if (remaining <= 0) { IsHolding = false; return DeathHoldPhase.ResetNow; }
            return DeathHoldPhase.Holding;
        }
    }
}
