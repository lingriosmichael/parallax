namespace Parallax.Core
{
    public enum TrapTriggerSource { Overlap, Chain }
    public enum TrapRepeatMode { Once, Rearm, Periodic }

    /// <summary>Pure, room-life-tick timing shared by every PAX-045 trap.</summary>
    public sealed class TrapTiming
    {
        readonly TrapTriggerSource source; readonly TrapRepeatMode repeat; readonly int delay; readonly int cooldown; readonly int period; readonly int phase;
        int pendingTick = -1, seenSourceTick = -1, armedSinceTick = -1;
        public bool IsArmed { get; private set; } = true;
        public bool JustRearmed { get; private set; }
        public bool IsEffectActive => LatestFireTick >= 0 && !IsArmed;
        public int LatestFireTick { get; private set; } = -1;
        public int TicksSinceFire(int roomTick) => LatestFireTick < 0 ? -1 : roomTick - LatestFireTick;

        public TrapTiming(TrapTriggerSource source, TrapRepeatMode repeat, int delay, int cooldown, int period, int phase)
        {
            this.source = source; this.repeat = repeat; this.delay = delay < 0 ? 0 : delay; this.cooldown = cooldown < 0 ? 0 : cooldown;
            this.period = period < 1 ? 1 : period; this.phase = phase < 0 ? 0 : phase;
        }

        public bool Step(int roomTick, bool overlapping, int sourceFireTick = -1)
        {
            JustRearmed = false;
            RearmIfDue(roomTick);
            // Consume a chain event even while cooling down: D-055 says it is ignored,
            // rather than deferred until the target rearms. A fire that arrives while an
            // earlier one is still pending is also ignored, so a fast source cannot keep
            // pushing the target's fire into the future.
            if (source == TrapTriggerSource.Chain && sourceFireTick != seenSourceTick)
            {
                seenSourceTick = sourceFireTick;
                if (IsArmed && sourceFireTick >= armedSinceTick && pendingTick < 0) pendingTick = sourceFireTick + delay;
            }
            if (!IsArmed) return false;
            if (repeat == TrapRepeatMode.Periodic)
                return roomTick >= phase && (roomTick - phase) % period == 0 && Fire(roomTick);
            if (source != TrapTriggerSource.Chain && pendingTick < 0 && overlapping) pendingTick = roomTick + delay;
            return pendingTick == roomTick && Fire(roomTick);
        }

        public void Reset() { IsArmed = true; JustRearmed = false; LatestFireTick = -1; pendingTick = -1; seenSourceTick = -1; armedSinceTick = -1; }

        bool Fire(int tick)
        {
            LatestFireTick = tick; pendingTick = -1;
            IsArmed = false;
            return true;
        }

        void RearmIfDue(int tick)
        {
            if ((repeat != TrapRepeatMode.Rearm && repeat != TrapRepeatMode.Periodic) || IsArmed || LatestFireTick < 0 || tick - LatestFireTick < cooldown) return;
            IsArmed = true; JustRearmed = true; armedSinceTick = tick;
        }
    }
}
