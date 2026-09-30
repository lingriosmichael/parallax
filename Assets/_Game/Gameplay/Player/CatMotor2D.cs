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
            body.rotation = Vector2.SignedAngle(Vector2.down, gravity.Direction);
        }

        /// <summary>PAX-086 (D-088): a geyser's push, called in the room step (after this tick's Step, before physics).
        /// Sets the velocity along `direction` to `speed` (absolute) and keeps the cross component; the cat is airborne
        /// and has no coyote left, so no coyote jump follows a launch. The jump buffer is untouched. Frozen: nothing.</summary>
        public void ApplyLaunch(Vector2 direction, float speed)
        {
            if (IsFrozen || body == null) return;
            climber?.Release();   // PAX-087 (D-089): a launch lets go of the vine (with the regrab lock)
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

        /// <summary>PAX-093 (D-095): a push wall's shove, called in the room step: the kit moves the cat by `delta` (no physics
        /// shove). It lets go of a vine. Frozen: nothing.</summary>
        public void ApplyPush(Vector2 delta)
        {
            if (IsFrozen || body == null) return;
            climber?.Release();
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

            Collider2D bodyCollider = null;
            foreach (Collider2D c in GetComponents<Collider2D>()) if (!c.isTrigger) { bodyCollider = c; break; }
            climber = new CatClimber(body, bodyCollider, config);
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

            float target = command.Move * config.MaxSpeed;
            float accelRate = Mathf.Abs(command.Move) > 0.01f ? config.Acceleration : config.Deceleration;
            along = Mathf.MoveTowards(along, target, accelRate * dt);

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

        void UpdateGrounded(Vector2 down, float fall)
        {
            int count = body.Cast(down, groundFilter, groundHits, config.GroundProbeDistance);

            bool touchingGround = false;
            Collider2D ground = null;
            for (int i = 0; i < count; i++)
            {
                if (Vector2.Dot(groundHits[i].normal, -down) > config.GroundNormalThreshold)
                {
                    touchingGround = true;
                    ground = groundHits[i].collider;
                    break;
                }
            }

            IsGrounded = touchingGround && fall >= -0.01f;
            GroundCollider = IsGrounded ? ground : null;
        }
    }
}
