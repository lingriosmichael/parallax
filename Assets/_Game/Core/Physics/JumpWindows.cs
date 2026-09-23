namespace Parallax.Core
{
    /// <summary>PAX-079 (D-077): coyote time and the jump buffer in whole ticks. One call per motor
    /// step, in this order: a press sets buffer = bufferTicks; a grounded step sets
    /// coyote = coyoteTicks; the jump fires if buffer &gt; 0 and (grounded or coyote &gt; 0); a jump
    /// sets both to 0; then buffer counts down by 1, and coyote counts down by 1 only on an airborne
    /// step, neither below 0. So the ground always allows a jump, coyote allows the first
    /// coyoteTicks airborne steps, and a press is honoured on its own step and the next
    /// bufferTicks - 1. It counts calls, never seconds.</summary>
    public static class JumpWindows
    {
        public static bool Step(ref int coyote, ref int buffer, bool grounded, bool pressed, int coyoteTicks, int bufferTicks)
        {
            if (pressed) buffer = bufferTicks;
            if (grounded) coyote = coyoteTicks;

            bool jump = buffer > 0 && (grounded || coyote > 0);
            if (jump)
            {
                buffer = 0;
                coyote = 0;
            }

            if (buffer > 0) buffer--;
            if (!grounded && coyote > 0) coyote--;
            return jump;
        }
    }
}
