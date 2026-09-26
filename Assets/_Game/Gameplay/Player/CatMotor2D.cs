using Parallax.Core;
using Parallax.Gameplay.Reality;
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

        public bool IsGrounded { get; private set; }
        public bool IsFrozen { get; private set; }

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
            body.rotation = Vector2.SignedAngle(Vector2.down, gravity.Direction);
        }

        /// <summary>PAX-086 (D-088): a geyser's push, called in the room step (after this tick's Step, before physics).
        /// Sets the velocity along `direction` to `speed` (absolute) and keeps the cross component; the cat is airborne
        /// and has no coyote left, so no coyote jump follows a launch. The jump buffer is untouched. Frozen: nothing.</summary>
        public void ApplyLaunch(Vector2 direction, float speed)
        {
            if (IsFrozen || body == null) return;
            body.linearVelocity = GeyserMath.Launch(body.linearVelocity, direction, speed);
            coyoteTimer = 0f;
            IsGrounded = false;
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
        }

        public void Step(in CatCommand input, float dt)
        {
            // PAX-047 (D-058): frozen during a death hold — the command is drained by the
            // driver upstream (so no source's latched edges leak past the hold) but discarded
            // here; velocity/rotation/gravity stay exactly as Freeze() left them.
            if (IsFrozen) return;

            SetCommand(input);

            gravity.FixedTick(dt);

            Vector2 down  = gravity.Direction;
            Vector2 right = new Vector2(-down.y, down.x); // down (0,-1) -> right (1,0)

            Vector2 v   = body.linearVelocity;
            float along = Vector2.Dot(v, right);
            float fall  = Vector2.Dot(v, down);

            UpdateGrounded(down, fall);

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
            for (int i = 0; i < count; i++)
            {
                if (Vector2.Dot(groundHits[i].normal, -down) > config.GroundNormalThreshold)
                {
                    touchingGround = true;
                    break;
                }
            }

            IsGrounded = touchingGround && fall >= -0.01f;
        }
    }
}
