using Parallax.Core;
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

        public void SetCommand(CatCommand cmd)
        {
            command = cmd;
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
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            gravity.FixedTick(dt);

            Vector2 down  = gravity.Direction;
            Vector2 right = new Vector2(-down.y, down.x); // down (0,-1) -> right (1,0)

            Vector2 v   = body.linearVelocity;
            float along = Vector2.Dot(v, right);
            float fall  = Vector2.Dot(v, down);

            float target = command.Move * config.MaxSpeed;
            float accelRate = Mathf.Abs(command.Move) > 0.01f ? config.Acceleration : config.Deceleration;
            along = Mathf.MoveTowards(along, target, accelRate * dt);

            fall += gravity.Strength * dt;
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
        }

        void UpdateFacing()
        {
            if (visual == null) return;
            if (Mathf.Abs(command.Move) <= 0.01f) return;

            Vector3 scale = visual.localScale;
            float absX = Mathf.Abs(scale.x);
            scale.x = command.Move > 0f ? absX : -absX;
            visual.localScale = scale;
        }
    }
}
