namespace Parallax.Core
{
    /// <summary>D-324 (PAX-129): the cat's two skills, in whole ticks (D-077), one call per motor step.
    /// The air jump: a jump press in the air, once the ground and coyote jumps are out, jumps again, AirJumps times before the
    /// cat is back on the ground, a wall or a vine. A press while the cat is about to land is left to the jump buffer, so it
    /// becomes the normal jump on landing and the air jump isn't spent.
    /// The whisker dodge: a press starts DodgeTicks during which arrows and lasers pass through the cat, and the next press
    /// counts only CooldownTicks after this one.</summary>
    public static class CatSkills
    {
        /// <summary>Whether this step's press is an air jump. `groundJumped`: JumpWindows fired this step.</summary>
        public static bool AirJump(bool pressed, bool groundJumped, bool grounded, bool landingSoon, int airJumpsLeft) =>
            pressed && !groundJumped && !grounded && !landingSoon && airJumpsLeft > 0;

        /// <summary>One step of the dodge: both counts go down by one (not below 0), then a press with the cooldown at 0 starts
        /// it (true): `active` is the dodge's ticks left including this one, `cooldown` the steps until a press counts again, so
        /// a dodge started on step n covers steps n to n + dodgeTicks - 1, and the next counts from step n + cooldownTicks.</summary>
        public static bool Dodge(ref int active, ref int cooldown, bool pressed, int dodgeTicks, int cooldownTicks)
        {
            if (cooldown > 0) cooldown--;
            if (active > 0) active--;
            bool start = pressed && cooldown <= 0 && dodgeTicks > 0;
            if (start)
            {
                active = dodgeTicks;
                cooldown = cooldownTicks;
            }
            return start;
        }

        /// <summary>How far the dodge has refilled, 0 (just used) to 1 (ready), for the button.</summary>
        public static float DodgeReadiness(int cooldown, int cooldownTicks) =>
            cooldownTicks <= 0 || cooldown <= 0 ? 1f : 1f - (float)cooldown / cooldownTicks;
    }
}
