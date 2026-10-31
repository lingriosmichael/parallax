using UnityEngine;

namespace Parallax.Core
{
    /// <summary>PAX-114/115/116 (D-187): the poses of the new movers as pure functions of ticks, shared by the runtime, the
    /// validators and the tests. A path's points are offsets from home, in order; home itself is the implicit first point.</summary>
    public static class TrapPath
    {
        /// <summary>How far along the move the mover is `ticksSinceFire` after its fire: 0 home, 1 the path's end, going out over
        /// moveTicks, holding, and back over returnTicks (TrapMotion.MovingOffset's timing).</summary>
        public static float Progress(int ticksSinceFire, int moveTicks, int holdTicks, int returnTicks) =>
            TrapMotion.MovingOffset(Vector2.right, ticksSinceFire, moveTicks, holdTicks, returnTicks).x;

        /// <summary>The path's length, home through every point.</summary>
        public static float Length(Vector2[] points)
        {
            float length = 0f; Vector2 from = Vector2.zero;
            if (points != null) foreach (Vector2 p in points) { length += (p - from).magnitude; from = p; }
            return length;
        }

        /// <summary>The offset `progress` (0-1) of the way along the path by arc length.</summary>
        public static Vector2 Along(Vector2[] points, float progress) => AlongFrom(points, 0, progress);

        /// <summary>D-250: the offset `progress` (0-1) of the way by arc length along the legs from point `first - 1` (home for 0) on.</summary>
        static Vector2 AlongFrom(Vector2[] points, int first, float progress)
        {
            if (points == null || points.Length == 0) return Vector2.zero;
            Vector2 origin = first > 0 ? points[first - 1] : Vector2.zero, from = origin;
            float total = 0f;
            for (int i = first; i < points.Length; i++) { total += (points[i] - from).magnitude; from = points[i]; }
            if (total <= 0f) return points[points.Length - 1];
            float d = Mathf.Clamp01(progress) * total;
            from = origin;
            for (int i = first; i < points.Length; i++)
            {
                Vector2 p = points[i];
                float leg = (p - from).magnitude;
                if (d <= leg) return leg <= 0f ? p : Vector2.Lerp(from, p, d / leg);
                d -= leg; from = p;
            }
            return points[points.Length - 1];
        }

        /// <summary>A glitch wall's point index (0 = home, n = the last point) at `progress`: it sits on a point and jumps to the
        /// next each 1/n of the way.</summary>
        public static int GlitchIndex(int count, float progress) => count <= 0 ? 0 : Mathf.Clamp(Mathf.FloorToInt(progress * count + 1e-4f), 0, count);

        /// <summary>The offset of point `index` (0 = home).</summary>
        public static Vector2 Point(Vector2[] points, int index) => index <= 0 || points == null || points.Length == 0 ? Vector2.zero : points[Mathf.Min(index, points.Length) - 1];

        /// <summary>The mover's offset `ticksSinceFire` after its fire: along the path, or on its points for a glitch wall.</summary>
        public static Vector2 Offset(Vector2[] points, bool glitch, int ticksSinceFire, int moveTicks, int holdTicks, int returnTicks, int firstLegTicks = 0)
        {
            float progress = Progress(ticksSinceFire, moveTicks, holdTicks, returnTicks);
            if (glitch) return Point(points, GlitchIndex(points?.Length ?? 0, progress));
            // D-250: the first leg over firstLegTicks of moveTicks, the rest over the remainder by arc length (0: all by arc length).
            if (firstLegTicks <= 0 || firstLegTicks >= moveTicks || points == null || points.Length < 2) return Along(points, progress);
            float t = Mathf.Clamp01(progress) * moveTicks;
            return t <= firstLegTicks ? points[0] * (t / firstLegTicks) : AlongFrom(points, 1, (t - firstLegTicks) / (moveTicks - firstLegTicks));
        }

        /// <summary>A glitch wall's warning: true, with the index it jumps to, when it jumps within `warnTicks` from now.</summary>
        public static bool GlitchWarning(Vector2[] points, int ticksSinceFire, int moveTicks, int holdTicks, int returnTicks, int warnTicks, out int next)
        {
            int count = points?.Length ?? 0;
            int now = GlitchIndex(count, Progress(ticksSinceFire, moveTicks, holdTicks, returnTicks));
            next = now;
            for (int w = 1; w <= warnTicks; w++)
            {
                int ahead = GlitchIndex(count, Progress(ticksSinceFire + w, moveTicks, holdTicks, returnTicks));
                if (ahead != now) { next = ahead; return true; }
            }
            return false;
        }

        /// <summary>PAX-115: a flip platform's turn (degrees, 0-180) `ticksSinceFire` after its fire.</summary>
        public static float FlipAngle(int ticksSinceFire, int flipTicks, int holdTicks, int returnTicks) =>
            180f * Progress(ticksSinceFire, flipTicks, holdTicks, returnTicks);

        /// <summary>PAX-116: a spinner's angle (degrees) `ticks` after it started turning.</summary>
        public static float SpinAngle(float startAngle, float degreesPerTick, int ticks) => Mathf.Repeat(startAngle + degreesPerTick * Mathf.Max(0, ticks), 360f);

        /// <summary>PAX-118 (D-197): a pendulum's angle (degrees) `ticks` after it started: `centre` ± `amplitude`, once every `period`.</summary>
        public static float SwingAngle(float centre, float amplitude, int period, int ticks) =>
            centre + amplitude * Mathf.Sin(2f * Mathf.PI * Mathf.Max(0, ticks) / Mathf.Max(1, period));

        /// <summary>PAX-122 (D-201): a phase platform's state `since` ticks after its fire: 0 gone, 1 there, 2 there and flickering
        /// (its last warnTicks before it goes). Negative `since` (never fired, or between cycles): gone.</summary>
        public static int PhaseState(int since, int onTicks, int warnTicks) =>
            since < 0 || since >= onTicks ? 0 : since >= onTicks - warnTicks ? 2 : 1;

        /// <summary>PAX-121 (D-197): a turret's phase `since` ticks after its fire: 0 aiming, 1 locked, 2 firing, 3 resting (-1: never fired).</summary>
        public static int TurretPhase(int since, int aim, int lockTicks, int fire) =>
            since < 0 ? -1 : since < aim ? 0 : since < aim + lockTicks ? 1 : since < aim + lockTicks + fire ? 2 : 3;

        /// <summary>D-203: whether an aiming turret tracks a cat at `toCat` from its muzzle: inside `zone` (relative to the muzzle; zero
        /// size: anywhere), and, ceiling-mounted, below the muzzle and in its sight (`reach`, the distance to the first solid toward
        /// the cat, reaches it).</summary>
        public static bool TurretTracks(Vector2 toCat, bool ceilingMount, Rect zone, float reach) =>
            (zone.width <= 0f || zone.height <= 0f || zone.Contains(toCat)) && (!ceilingMount || (toCat.y < 0f && reach >= toCat.magnitude));

        /// <summary>PAX-116: the centre of arm `arm` of `arms` (evenly spread) for a spinner at `angle` about `pivot`.</summary>
        public static Vector2 ArmCentre(Vector2 pivot, float angle, int arm, int arms, float armLength)
        {
            float a = (angle + arm * 360f / Mathf.Max(1, arms)) * Mathf.Deg2Rad;
            return pivot + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (armLength * .5f);
        }

        /// <summary>PAX-116: arm `arm`'s angle (degrees) for a spinner at `angle`.</summary>
        public static float ArmAngle(float angle, int arm, int arms) => angle + arm * 360f / Mathf.Max(1, arms);

        /// <summary>PAX-123 (D-276): a trident's beam `i` (0, 1, 2): along `down`, then `spread` degrees to either side of it.</summary>
        public static float TridentAngle(float down, float spread, int i) => i == 0 ? down : i == 1 ? down - spread : down + spread;

        /// <summary>PAX-124 (D-276): how far along its path a rotating chaser's pivot is `since` ticks after its fire: still through its
        /// warning, then evenly over `moveTicks`, then held at the end (0..1; 0 before the fire).</summary>
        public static float SweepProgress(int since, int warnTicks, int moveTicks) =>
            since < warnTicks ? 0f : moveTicks <= 0 ? 1f : Mathf.Clamp01((since - warnTicks) / (float)moveTicks);

        /// <summary>PAX-124: its bar's angle: `start` until its warning ends, then turning `degreesPerTick` (it turns as it moves).</summary>
        public static float SweepAngle(float start, float degreesPerTick, int since, int warnTicks) => SpinAngle(start, degreesPerTick, since - warnTicks);

        /// <summary>D-276 amendment 1: a bar that holds `start` until `spinFrom` ticks after its fire, then starts turning, speeding up
        /// evenly over `ramp` ticks to `degreesPerTick` (0: at once), and turns on at that rate.</summary>
        public static float SweepAngle(float start, float degreesPerTick, int since, int spinFrom, int ramp)
        {
            int t = since - spinFrom;
            if (t <= 0) return start;
            float turned = ramp > 0 && t < ramp ? degreesPerTick * t * t / (2f * ramp) : degreesPerTick * ((ramp > 0 ? ramp * .5f : 0f) + (t - Mathf.Max(ramp, 0)));
            return Mathf.Repeat(start + turned, 360f);
        }
    }
}
