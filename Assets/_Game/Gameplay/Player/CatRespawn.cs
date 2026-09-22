using Parallax.Gameplay.GravityControl;
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

        public event System.Action Respawned;

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

            // PAX-047 (D-058): a no-op unless the cat is actually frozen (co-op/dev respawns
            // never call Freeze(), so this changes nothing for them).
            motor.Unfreeze();

            CatSeat seat = GetComponent<CatSeat>();
            if (seat != null) seat.Release();

            gravity.SetTargetDirection(gravityDirection, snap: true);

            body.position = position;
            transform.position = position;

            motor.ResetMotion();

            Debug.Log($"CatRespawn: '{gameObject.name}' respawned at {position}.", this);

            if (Respawned != null) Respawned.Invoke();
        }
    }
}
