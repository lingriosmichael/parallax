namespace Parallax.Core
{
    /// <summary>PAX-085 (D-087): the inverter's timer, in room ticks. Fired in the room step at tick F, it inverts the
    /// motor steps that belong to room ticks F+1 through F+duration (the room step runs after that tick's motor step).
    /// The cue is shown from F through F+duration and blinks over the last BlinkTicks of that span. Refiring restarts
    /// the window; Clear (room reset, room no longer live) ends it.</summary>
    public sealed class ControlInversion
    {
        public const int DefaultDurationTicks = 150;
        public const int BlinkTicks = 30;
        public const int BlinkHalfPeriodTicks = 5;

        int fireTick = -1, duration;

        public bool HasFired => fireTick >= 0;
        public int FireTick => fireTick;

        public void Fire(int tick, int durationTicks)
        {
            fireTick = tick;
            duration = durationTicks < 0 ? 0 : durationTicks;
        }

        public void Clear()
        {
            fireTick = -1;
            duration = 0;
        }

        // Whether the motor step that belongs to room tick `tick` is inverted.
        public bool IsActive(int tick) => fireTick >= 0 && tick > fireTick && tick <= fireTick + duration;

        // Inverted motor steps still to come after room tick `tick`.
        public int Remaining(int tick)
        {
            if (fireTick < 0) return 0;
            int left = fireTick + duration - tick;
            return left < 0 ? 0 : left > duration ? duration : left;
        }

        // The cue at the end of room tick `tick`: on from the fire tick through the last inverted step; over the last
        // BlinkTicks it is off, on, off... in BlinkHalfPeriodTicks runs, ending on.
        public bool IsCueVisible(int tick)
        {
            if (fireTick < 0 || tick < fireTick || tick > fireTick + duration) return false;
            int left = fireTick + duration - tick;
            if (left >= BlinkTicks) return true;
            int intoBlink = BlinkTicks - 1 - left;
            return (intoBlink / BlinkHalfPeriodTicks) % 2 == 1;
        }
    }
}
