using Parallax.Core;
using Parallax.Gameplay.Reality;
using UnityEngine;

namespace Parallax.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(GravityReceiver))]
    public sealed class CatMotor2D : MonoBehaviour
    {
        [SerializeField] CatMotorConfig config;
        [SerializeField] Transform visual;

        Rigidbody2D body;
        GravityReceiver gravity;
        CatCommand command;
        bool warnedGravityScaleChanged;

        ContactFilter2D groundFilter;
        readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];

        float coyoteTimer;
        float jumpBufferTimer;

        public bool IsGrounded { get; private set; }
        public bool FacingRight => visual == null || visual.localScale.x >= 0f;

        public void SetFacing(bool facingRight)
        {
            if (visual == null) return;
            Vector3 scale = visual.localScale;
            float absX = Mathf.Abs(scale.x);
            scale.x = facingRight ? absX : -absX;
            visual.localScale = scale;
        }

        void SetCommand(CatCommand cmd)
        {
            command = cmd;

            if (config != null && cmd.JumpPressed)
            {
                jumpBufferTimer = config.JumpBufferTime;
            }
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
            SetCommand(input);

            gravity.FixedTick(dt);

            Vector2 down  = gravity.Direction;
            Vector2 right = new Vector2(-down.y, down.x); // down (0,-1) -> right (1,0)

            Vector2 v   = body.linearVelocity;
            float along = Vector2.Dot(v, right);
            float fall  = Vector2.Dot(v, down);

            UpdateGrounded(down, fall);

            coyoteTimer = IsGrounded ? config.CoyoteTime : Mathf.Max(0f, coyoteTimer - dt);
            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - dt);

            float target = command.Move * config.MaxSpeed;
            float accelRate = Mathf.Abs(command.Move) > 0.01f ? config.Acceleration : config.Deceleration;
            along = Mathf.MoveTowards(along, target, accelRate * dt);

            fall += gravity.Strength * dt;

            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                fall = -JumpMath.SpeedForHeight(config.JumpHeight, gravity.Strength);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
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

            UpdateFacing();

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

        void UpdateFacing()
        {
            if (visual == null) return;
            if (Mathf.Abs(command.Move) <= 0.01f) return;

            SetFacing(command.Move > 0f);
        }
    }
}
