using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Gameplay.Player
{
    /// <summary>PAX-087 (D-089): the motor's climbing branch, a plain class CatMotor2D owns (not a component, so the cat
    /// prefab is unchanged). The vines come from LocalHumanDriver.Activate; with none, or with Climb 0 and not
    /// climbing, Step returns false before touching the body or the jump timers, so the motor's normal path runs exactly
    /// as before.</summary>
    public sealed class CatClimber
    {
        readonly Rigidbody2D body;
        readonly Collider2D collider;
        readonly CatMotorConfig config;
        readonly ClimbState state = new();
        ClimbVine[] vines = System.Array.Empty<ClimbVine>();

        public CatClimber(Rigidbody2D body, Collider2D collider, CatMotorConfig config)
        {
            this.body = body; this.collider = collider; this.config = config;
        }

        public bool IsClimbing => state.IsClimbing;
        public ClimbVine Vine => state.IsClimbing ? vines[state.Vine] : null;

        public void SetVines(ClimbVine[] found)
        {
            state.Clear();
            vines = found ?? System.Array.Empty<ClimbVine>();
        }

        /// <summary>A release with the regrab lock (a launch, a snap). Nothing unless climbing `vine` (any vine if null).</summary>
        public void Release(ClimbVine vine = null)
        {
            if (!state.IsClimbing || (vine != null && vines[state.Vine] != vine)) return;
            state.Release(config.RegrabLockTicks);
        }

        public void Clear() => state.Clear();

        /// <summary>One motor step, after the ground test. True: this step is the climber's (climbing, or a leap off the
        /// vine) and the body's velocity is set; false: the motor's normal path runs (also on the step a release happens,
        /// so the cat falls from the velocity it had). `buffered`: the motor's jump buffer is open, which leaps on the
        /// grab tick only (while climbing, a press leaps at once).</summary>
        public bool Step(in CatCommand command, Vector2 down, Vector2 right, bool grounded, bool buffered, float jumpSpeed, float dt)
        {
            state.BeginStep();
            if (vines.Length == 0 || collider == null) return false;
            bool gravityUp = down.y > 0f;

            if (state.IsClimbing)
            {
                ClimbVine current = vines[state.Vine];
                if (current == null || !current.IsGrabbable || state.GrabbedWithGravityUp != gravityUp)
                { state.Release(config.RegrabLockTicks); return false; }
            }
            else if (!TryGrab(command.Climb, grounded, gravityUp)) return false;
            else if (buffered) return Leap(command, down, right, jumpSpeed);

            Rect vine = vines[state.Vine].GrabRect;
            if (command.JumpPressed) return Leap(command, down, right, jumpSpeed);

            Bounds b = collider.bounds;
            if (ClimbState.ReleasesAtBottom(command.Climb, b.center.y, vine.yMin, grounded))
            { state.Release(config.RegrabLockTicks); return false; }

            body.linearVelocity = new Vector2(0f, ClimbState.ClimbVelocity(command.Climb, config.ClimbSpeed, b.max.y, vine.yMax, dt));
            return true;
        }

        bool Leap(in CatCommand command, Vector2 down, Vector2 right, float jumpSpeed)
        {
            body.linearVelocity = ClimbState.LeapVelocity(command.Move, right, down, config.MaxSpeed, jumpSpeed);
            state.Release(config.RegrabLockTicks);
            return true;
        }

        bool TryGrab(float climb, bool grounded, bool gravityUp)
        {
            if (!ClimbState.WantsGrab(climb, grounded, config.GrabThreshold)) return false;
            Rect cat = ToRect(collider.bounds);
            for (int i = 0; i < vines.Length; i++)
            {
                ClimbVine vine = vines[i];
                if (vine == null || !vine.IsGrabbable || !vine.GrabRect.Overlaps(cat)) continue;
                if (!state.TryGrab(i, climb, grounded, config.GrabThreshold, gravityUp)) continue;
                // x snaps to the vine's centre on the grab tick.
                Vector2 position = body.position;
                position.x += vine.GrabRect.center.x - cat.center.x;
                body.position = position;
                return true;
            }
            return false;
        }

        static Rect ToRect(Bounds b) => Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y);
    }
}
