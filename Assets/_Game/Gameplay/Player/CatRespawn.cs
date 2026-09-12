using UnityEngine;

namespace Parallax.Gameplay.Player
{
    [RequireComponent(typeof(CatMotor2D), typeof(GravityReceiver))]
    public sealed class CatRespawn : MonoBehaviour
    {
        Rigidbody2D body;
        CatMotor2D motor;
        GravityReceiver gravity;

        int lastRespawnFrame = -1;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            motor = GetComponent<CatMotor2D>();
            gravity = GetComponent<GravityReceiver>();
        }

        public void RespawnAt(Vector2 position, Vector2 gravityDirection)
        {
            if (lastRespawnFrame == Time.frameCount) return;
            lastRespawnFrame = Time.frameCount;

            gravity.SetTargetDirection(gravityDirection, snap: true);

            body.position = position;
            transform.position = position;

            motor.ResetMotion();

            Debug.Log($"CatRespawn: '{gameObject.name}' respawned at {position}.", this);
        }
    }
}
