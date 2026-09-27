namespace Parallax.Core
{
    public enum TrapCountdownState { Armed, Counting, Fired }

    /// <summary>PAX-090 (D-091): every field TrapCountdown changes after construction, by value.</summary>
    public struct TrapCountdownSnapshot
    {
        public TrapCountdownState State;
        public int Elapsed, TicksSinceFired;
    }

    public sealed class TrapCountdown
    {
        readonly int delayTicks;
        readonly bool rearmOnExit;
        int elapsed;

        public TrapCountdownState State { get; private set; } = TrapCountdownState.Armed;
        public int TicksSinceFired { get; private set; }

        public TrapCountdown(int delayTicks, bool rearmOnExit = false)
        {
            this.delayTicks = delayTicks < 0 ? 0 : delayTicks;
            this.rearmOnExit = rearmOnExit;
        }

        public bool Step(bool triggered)
        {
            if (State == TrapCountdownState.Fired)
            {
                TicksSinceFired++;
                if (rearmOnExit && !triggered) Reset();
                return false;
            }
            if (State == TrapCountdownState.Armed && triggered)
            {
                if (delayTicks == 0) return Fire();
                State = TrapCountdownState.Counting;
                elapsed = 0;
                return false;
            }
            if (State != TrapCountdownState.Counting) return false;
            elapsed++;
            return elapsed >= delayTicks && Fire();
        }

        // PAX-090 (D-091): capture and restore only; no other member's behaviour changes.
        public TrapCountdownSnapshot Capture() => new() { State = State, Elapsed = elapsed, TicksSinceFired = TicksSinceFired };

        public void Restore(in TrapCountdownSnapshot snapshot)
        {
            State = snapshot.State; elapsed = snapshot.Elapsed; TicksSinceFired = snapshot.TicksSinceFired;
        }

        public void Reset()
        {
            State = TrapCountdownState.Armed;
            elapsed = 0;
            TicksSinceFired = 0;
        }

        bool Fire()
        {
            State = TrapCountdownState.Fired;
            TicksSinceFired = 0;
            return true;
        }
    }
}
