using Parallax.Core;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(GravityReceiver))]
    public sealed class CatMotor2D : MonoBehaviour
    {
        [SerializeField] CatMotorConfig config;

        Rigidbody2D body;
        GravityReceiver gravity;
        CatCommand command;
        bool warnedGravityScaleChanged;

        ContactFilter2D groundFilter;
        readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];

        // PAX-079 (D-077): whole ticks remaining, 0 = closed; JumpWindows does the arithmetic in int.
        // Kept as floats with these names because CatMotor2DFreezeTests (reads jumpBufferTimer) and
        // PauseInputTests (sets coyoteTimer) reach them by reflection.
        float coyoteTimer;
        float jumpBufferTimer;

        RigidbodyConstraints2D savedConstraints;
        // PAX-087 (D-089): the climbing branch; null only when Awake disabled the motor.
        CatClimber climber;
        // PAX-105 (D-110): the wall branch; null only when Awake disabled the motor.
        CatWallCling wallCling;
        // D-119: the body collider (the ground tie-break measures overlap with it).
        Collider2D bodyCollider;

        public bool IsGrounded { get; private set; }
        /// <summary>PAX-093 (D-095): the collider this step's ground test stood the cat on (null when not grounded).</summary>
        public Collider2D GroundCollider { get; private set; }
        /// <summary>PAX-093 (D-095): true on the step a grounded cat jumped (not a leap off a vine).</summary>
        public bool JumpedThisStep { get; private set; }
        /// <summary>PAX-A14 (presentation only, read by CatVisualPresenter): this step's Carry floor velocity (zero when no
        /// floor carried the cat), the position ApplyCarry wrote (zero when none: a position write drops Rigidbody2D
        /// interpolation for the step), and the body's position when the step began. Written only; never read by motion.</summary>
        public Vector2 CarrierVelocity { get; private set; }
        public Vector2 CarryShift { get; private set; }
        public Vector2 StepStartPosition { get; private set; }
        public bool IsFrozen { get; private set; }
        public bool IsClimbing => climber != null && climber.IsClimbing;
        public ClimbVine ClimbedVine => climber?.Vine;
        /// <summary>PAX-105 (D-110): clinging to a wall, and on which side (+1 right, -1 left, 0 not clinging).</summary>
        public bool IsClinging => wallCling != null && wallCling.IsClinging;
        public int ClingSide => wallCling != null ? wallCling.ClingSide : 0;
        /// <summary>PAX-105 (D-110): true on the step the cat jumped off a wall.</summary>
        public bool WallJumpedThisStep => wallCling != null && wallCling.WallJumpedThisStep;
        /// <summary>PAX-105 (presentation only, read by CatVisualPresenter): the collider clung to (null when not clinging).</summary>
        public Collider2D ClingFace => wallCling?.Face;

        /// <summary>PAX-087 (D-089): the vines this cat may climb, handed over by LocalHumanDriver.Activate (null: none).
        /// Any climb in progress ends.</summary>
        public void SetClimbVines(ClimbVine[] vines) => climber?.SetVines(vines);

        /// <summary>PAX-087 (D-089): a snap vine lets go of the cat (with the regrab lock). Nothing unless climbing it.</summary>
        public void ReleaseClimb(ClimbVine vine) => climber?.Release(vine);

        /// <summary>PAX-047 (D-058): freezes the cat in place for a death hold. Step() no-ops while frozen.</summary>
        public void Freeze()
        {
            if (IsFrozen) return;
            IsFrozen = true;
            savedConstraints = body.constraints;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        public void Unfreeze()
        {
            if (!IsFrozen) return;
            IsFrozen = false;
            body.constraints = savedConstraints;
        }

        void SetCommand(CatCommand cmd)
        {
            command = cmd;
        }

        public void ResetMotion()
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            command = CatCommand.None;
            climber?.Clear();   // PAX-087 (D-089): a death, reset or respawn ends a climb, with no regrab lock
            wallCling?.Clear(); // PAX-105 (D-110): and a cling and the face lock
            body.rotation = Vector2.SignedAngle(Vector2.down, gravity.Direction);
        }

        /// <summary>PAX-086 (D-088): a geyser's push, called in the room step (after this tick's Step, before physics).
        /// Sets the velocity along `direction` to `speed` (absolute) and keeps the cross component; the cat is airborne
        /// and has no coyote left, so no coyote jump follows a launch. The jump buffer is untouched. Frozen: nothing.</summary>
        public void ApplyLaunch(Vector2 direction, float speed)
        {
            if (IsFrozen || body == null) return;
            climber?.Release();   // PAX-087 (D-089): a launch lets go of the vine (with the regrab lock)
            wallCling?.Release(); // PAX-105 (D-110): and of a wall (the face lock)
            body.linearVelocity = GeyserMath.Launch(body.linearVelocity, direction, speed);
            coyoteTimer = 0f;
            IsGrounded = false;
        }

        /// <summary>PAX-093 (D-095): a Carry floor's move, called in the room step (after this tick's Step, before physics),
        /// for a cat grounded on it (MovingFloorMath.Carry; the part into the cat stays physics'). The part along the ground
        /// moves the cat by position; the part away from the ground raises its fall speed to at least the floor's for this
        /// step, so physics moves it with the floor (a position shift there would start the step inside the floor, and the
        /// solver would push it back out). On the step the cat jumped, the floor's sideways speed is added to its velocity
        /// instead, once (Q3). Frozen, climbing or not grounded: nothing.</summary>
        public void ApplyCarry(Vector2 displacement, float dt)
        {
            if (IsFrozen || body == null || IsClimbing || !IsGrounded || dt <= 0f) return;
            Vector2 down = gravity.Direction, right = new(-down.y, down.x);
            Vector2 carry = MovingFloorMath.Carry(displacement, down);
            float along = Vector2.Dot(carry, right), away = Vector2.Dot(carry, down);
            CarrierVelocity = displacement / dt;
            if (JumpedThisStep) { body.linearVelocity += right * (along / dt); return; }
            body.position += right * along;
            CarryShift = right * along;
            Vector2 v = body.linearVelocity;
            float fall = Vector2.Dot(v, down);
            if (away > 0f && fall < away / dt) body.linearVelocity = v + down * (away / dt - fall);
        }

        /// <summary>D-115 (supersedes D-056 (3)'s launch): a floor that was carrying the cat upward and slows or stops this room
        /// tick leaves it riding at the floor's new speed instead of flinging it on (the push into the cat is physics', so the
        /// cat kept the floor's old speed). Only a cat touching the floor's top, rising no faster than the floor was (a jump
        /// is faster) and that didn't jump this step. Called in the room step, before physics.</summary>
        public void MatchSlowingFloor(Collider2D floor, Vector2 displacement, Vector2 previousDisplacement, float dt)
        {
            if (IsFrozen || body == null || floor == null || IsClimbing || JumpedThisStep || WallJumpedThisStep || dt <= 0f) return;
            Vector2 down = gravity.Direction;
            float before = -Vector2.Dot(previousDisplacement, down) / dt, now = Mathf.Max(0f, -Vector2.Dot(displacement, down) / dt);
            if (before <= now + 1e-4f) return;
            Vector2 v = body.linearVelocity;
            float rise = -Vector2.Dot(v, down);
            if (rise <= now + 1e-4f || rise > before + 2f) return;
            int count = body.Cast(down, groundFilter, groundHits, config.GroundProbeDistance);
            for (int i = 0; i < count; i++)
                if (groundHits[i].collider == floor && Vector2.Dot(groundHits[i].normal, -down) > config.GroundNormalThreshold)
                {
                    body.linearVelocity = v + down * (rise - now);
                    return;
                }
        }

        /// <summary>PAX-093 (D-095): a push wall's shove, called in the room step: the kit moves the cat by `delta` (no physics
        /// shove). It lets go of a vine. Frozen: nothing.</summary>
        public void ApplyPush(Vector2 delta)
        {
            if (IsFrozen || body == null) return;
            climber?.Release();
            wallCling?.Release();   // PAX-105 (D-110)
            body.position += delta;
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            gravity = GetComponent<GravityReceiver>();
            body.gravityScale = 0f;

            if (config == null)
            {
                Debug.LogError($"CatMotor2D on '{gameObject.name}' has no CatMotorConfig assigned. Disabling component.", this);
                enabled = false;
                return;
            }

            var reality = GetComponentInParent<RealityRoot>();
            if (reality == null)
            {
                Debug.LogError($"CatMotor2D on '{gameObject.name}' has no RealityRoot in its parent hierarchy. Ground detection will hit nothing.", this);
            }

            groundFilter = new ContactFilter2D();
            groundFilter.useTriggers = false;
            groundFilter.SetLayerMask(reality != null ? reality.PhysicsMask : (LayerMask)0);

            bodyCollider = null;
            foreach (Collider2D c in GetComponents<Collider2D>()) if (!c.isTrigger) { bodyCollider = c; break; }
            climber = new CatClimber(body, bodyCollider, config);
            wallCling = new CatWallCling(body, bodyCollider, config, groundFilter);
        }

        public void Step(in CatCommand input, float dt)
        {
            // PAX-047 (D-058): frozen during a death hold — the command is drained by the
            // driver upstream (so no source's latched edges leak past the hold) but discarded
            // here; velocity/rotation/gravity stay exactly as Freeze() left them.
            JumpedThisStep = false;
            CarrierVelocity = CarryShift = Vector2.zero;
            if (body != null) StepStartPosition = body.position;
            if (IsFrozen) return;

            SetCommand(input);

            gravity.FixedTick(dt);

            Vector2 down  = gravity.Direction;
            Vector2 right = new Vector2(-down.y, down.x); // down (0,-1) -> right (1,0)

            Vector2 v   = body.linearVelocity;
            float along = Vector2.Dot(v, right);
            float fall  = Vector2.Dot(v, down);

            UpdateGrounded(down, fall);

            // PAX-087 (D-089): climbing, or a leap off a vine, owns this step. With Climb 0 and no climb in progress it
            // returns false having touched nothing, so everything below runs exactly as before.
            if (climber != null && climber.Step(command, down, right, IsGrounded, jumpBufferTimer > 0f, JumpMath.SpeedForHeight(config.JumpHeight, gravity.Strength), dt))
            {
                wallCling?.Release();   // PAX-105 (D-110): climbing a vine wins over clinging
                IsGrounded = false;
                coyoteTimer = 0f;
                jumpBufferTimer = 0f;
                body.rotation = Vector2.SignedAngle(Vector2.down, down);
                return;
            }

            // PAX-105 (D-110): a cling, or a wall jump, owns this step. Without a Grab press remembered, latch mode, a cling
            // or a move lock it returns false having touched no body, so everything below runs exactly as before.
            if (wallCling != null && wallCling.Step(command, down, IsGrounded, jumpBufferTimer > 0f, JumpMath.SpeedForHeight(config.JumpHeight, gravity.Strength), gravity.Strength, dt))
            {
                IsGrounded = false;
                coyoteTimer = 0f;
                jumpBufferTimer = 0f;
                body.rotation = Vector2.SignedAngle(Vector2.down, down);
                return;
            }

            int coyote = (int)coyoteTimer;
            int buffer = (int)jumpBufferTimer;
            bool jump = JumpWindows.Step(ref coyote, ref buffer, IsGrounded, command.JumpPressed,
                TickTime.ToWholeTicks(config.CoyoteTime), TickTime.ToWholeTicks(config.JumpBufferTime));
            coyoteTimer = (float)coyote;
            jumpBufferTimer = (float)buffer;

            float move = command.Move;
            bool keepAlong = false;
            // PAX-105 (D-110): after a wall jump, Move back toward the wall is ignored and, with no Move away, the sideways
            // speed is kept (no deceleration) until the move lock ends.
            if (wallCling != null && wallCling.TakeMoveLockStep())
            {
                move = wallCling.LockedMove(move);
                keepAlong = Mathf.Abs(move) <= 0.01f;
            }
            float target = move * config.MaxSpeed;
            float accelRate = Mathf.Abs(move) > 0.01f ? config.Acceleration : config.Deceleration;
            if (!keepAlong) along = Mathf.MoveTowards(along, target, accelRate * dt);

            fall += gravity.Strength * dt;

            if (jump)
            {
                fall = -JumpMath.SpeedForHeight(config.JumpHeight, gravity.Strength);
                JumpedThisStep = true;
            }

            fall = Mathf.Min(fall, config.MaxFallSpeed);

            body.linearVelocity = right * along + down * fall;

            if (body.gravityScale != 0f)
            {
                if (!warnedGravityScaleChanged)
                {
                    Debug.LogWarning($"CatMotor2D on '{gameObject.name}' found Rigidbody2D.gravityScale != 0. Forcing back to 0.", this);
                    warnedGravityScaleChanged = true;
                }
                body.gravityScale = 0f;
            }

            // Align body so local up = -gravity.Direction. Smoothing comes from GravityReceiver
            // turning gradually toward its target; this is a direct assignment, not a second smoothing layer.
            body.rotation = Vector2.SignedAngle(Vector2.down, down);
        }

        /// <summary>D-119: the floor a ground cast stands the cat on (null: none faces against gravity). A cat on a seam touches
        /// two floors at the same distance, and the cast's order between them isn't stable from one session to the next (a
        /// replay then reported a different ground): the nearer wins; on a tie (within 1e-3 u), the one under the cat's centre,
        /// then the one it overlaps more, then the name. The route harness records the ground through this too.</summary>
        public Collider2D BestGround(RaycastHit2D[] hits, int count, Vector2 down)
        {
            Collider2D ground = null;
            float distance = 0f;
            for (int i = 0; i < count; i++)
            {
                if (Vector2.Dot(hits[i].normal, -down) <= config.GroundNormalThreshold) continue;
                Collider2D c = hits[i].collider;
                if (ground == null || hits[i].distance < distance - 1e-3f || (hits[i].distance <= distance + 1e-3f && PreferredGround(c, ground)))
                { ground = c; distance = hits[i].distance; }
            }
            return ground;
        }

        // D-119: the seam tie-break: true when `a` is the better ground than `b`.
        bool PreferredGround(Collider2D a, Collider2D b)
        {
            float x = body.position.x;
            Bounds ba = a.bounds, bb = b.bounds, cat = bodyCollider != null ? bodyCollider.bounds : new Bounds(body.position, Vector3.zero);
            bool underA = x >= ba.min.x && x <= ba.max.x, underB = x >= bb.min.x && x <= bb.max.x;
            if (underA != underB) return underA;
            float overlapA = Mathf.Min(ba.max.x, cat.max.x) - Mathf.Max(ba.min.x, cat.min.x);
            float overlapB = Mathf.Min(bb.max.x, cat.max.x) - Mathf.Max(bb.min.x, cat.min.x);
            if (Mathf.Abs(overlapA - overlapB) > 1e-4f) return overlapA > overlapB;
            return string.CompareOrdinal(a.name, b.name) < 0;
        }

        void UpdateGrounded(Vector2 down, float fall)
        {
            int count = body.Cast(down, groundFilter, groundHits, config.GroundProbeDistance);

            Collider2D ground = BestGround(groundHits, count, down);
            bool touchingGround = ground != null;

            IsGrounded = touchingGround && fall >= -0.01f;
            GroundCollider = IsGrounded ? ground : null;
        }
    }
}
