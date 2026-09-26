using UnityEngine;

namespace Parallax.Core
{
    /// <summary>PAX-087 (D-089): the climbing rules, pure. CatClimber owns one per cat and feeds it motor steps.
    /// Grab: a grounded cat needs Climb ≥ +threshold (up only), an airborne cat |Climb| ≥ threshold, never while the
    /// same vine is locked. Climbing: screen-vertical speed Climb × speed, stopped so the collider's top never rises
    /// past the vine's top (the cat stays on the vine); no gravity. Leap: the jump launch plus sign(Move) × MaxSpeed. A release (other than a reset)
    /// locks that vine for the next lockTicks motor steps, counted by BeginStep, whether it happens inside or after one.</summary>
    public sealed class ClimbState
    {
        public const float DefaultClimbSpeed = 4f;
        public const int DefaultRegrabLockTicks = 10;
        public const float DefaultGrabThreshold = .5f;

        int step;
        int lockedVine = -1;
        int lockedUntilStep = int.MinValue;

        public bool IsClimbing { get; private set; }
        /// <summary>The vine being climbed (the caller's index), -1 when not climbing.</summary>
        public int Vine { get; private set; } = -1;
        /// <summary>The gravity side at the grab; a different side releases (a flip).</summary>
        public bool GrabbedWithGravityUp { get; private set; }

        /// <summary>Once per motor step, before any grab or climb decision.</summary>
        public void BeginStep() => step++;

        public static bool WantsGrab(float climb, bool grounded, float threshold) =>
            grounded ? climb >= threshold : Mathf.Abs(climb) >= threshold;

        public bool IsLocked(int vine) => vine == lockedVine && step <= lockedUntilStep;

        public bool TryGrab(int vine, float climb, bool grounded, float threshold, bool gravityUp)
        {
            if (IsClimbing || vine < 0 || IsLocked(vine) || !WantsGrab(climb, grounded, threshold)) return false;
            IsClimbing = true;
            Vine = vine;
            GrabbedWithGravityUp = gravityUp;
            return true;
        }

        /// <summary>Lets go of the vine and locks it for the next lockTicks motor steps. Nothing if not climbing.</summary>
        public void Release(int lockTicks)
        {
            if (!IsClimbing) return;
            lockedVine = Vine;
            lockedUntilStep = step + lockTicks;
            IsClimbing = false;
            Vine = -1;
        }

        /// <summary>A death, room reset or respawn: not climbing and nothing locked.</summary>
        public void Clear()
        {
            IsClimbing = false;
            Vine = -1;
            lockedVine = -1;
            lockedUntilStep = int.MinValue;
        }

        /// <summary>Screen-vertical velocity while climbing: Climb × speed, with the collider's top stopped at the vine's
        /// top, so the cat never climbs off the vine (never pushed down by the stop). Downward has no stop here; the bottom
        /// rule releases instead.</summary>
        public static float ClimbVelocity(float climb, float speed, float colliderTop, float vineTop, float dt)
        {
            float v = climb * speed;
            if (v <= 0f || dt <= 0f) return v;
            return Mathf.Min(v, Mathf.Max(0f, (vineTop - colliderTop) / dt));
        }

        /// <summary>R5 (2): climbing down releases once the collider's centre is below the vine's bottom, or when the
        /// cat stands on ground while pushing down.</summary>
        public static bool ReleasesAtBottom(float climb, float colliderCentreY, float vineBottom, bool grounded) =>
            climb < 0f && (colliderCentreY < vineBottom || grounded);

        /// <summary>The leap: the normal jump launch against gravity plus sign(move) × maxSpeed along the cat's right
        /// (straight up when move is 0). Move is the motor's (after the inverter, D-087).</summary>
        public static Vector2 LeapVelocity(float move, Vector2 right, Vector2 down, float maxSpeed, float jumpSpeed)
        {
            float side = Mathf.Abs(move) > .01f ? Mathf.Sign(move) : 0f;
            return right * (side * maxSpeed) - down * jumpSpeed;
        }
    }
}
