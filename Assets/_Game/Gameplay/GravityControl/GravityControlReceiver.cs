using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Transport;
using UnityEngine;

namespace Parallax.Gameplay.GravityControl
{
    public sealed class GravityControlReceiver : MonoBehaviour
    {
        [SerializeField] TransportHost transportHost;
        [SerializeField] float maxAngleDeg = 90f;
        [SerializeField] float snapDeg;
        RealityRoot root;
        GravityReceiver gravity;
        IRealityTransport transport;
        CatRespawn respawn;
        public float LastValue { get; private set; }
        public bool HasValue { get; private set; }
        void OnEnable() => EnsureInit();
        void OnDisable()
        {
            if (transport != null) transport.ControlReceived -= OnControl;
            if (respawn != null) respawn.Respawned -= ReassertHeldValue;
            transport = null;
            respawn = null;
        }
        void EnsureInit()
        {
            if (root == null) root = GetComponentInParent<RealityRoot>();
            if (gravity == null) gravity = GetComponent<GravityReceiver>();
            if (respawn == null)
            {
                respawn = GetComponent<CatRespawn>();
                if (respawn != null) respawn.Respawned += ReassertHeldValue;
            }
            if (transport != null || transportHost == null) return;
            transport = transportHost.Transport;
            if (transport != null) transport.ControlReceived += OnControl;
        }
        void FixedUpdate() => EnsureInit();
        void OnControl(ControlSample sample)
        {
            if (root == null || gravity == null || sample.Channel != ControlChannel.GravityAngle || sample.Target != root.Id) return;
            LastValue = sample.Value;
            HasValue = true;
            gravity.SetTargetDirection(GravityControlMapping.ToDirection(sample.Value, maxAngleDeg, snapDeg));
        }

        public void ReassertHeldValue()
        {
            if (!HasValue || gravity == null) return;
            Vector2 heldDirection = GravityControlMapping.ToDirection(LastValue, maxAngleDeg, snapDeg);
            Vector2 direction = GravityRespawnPrecedence.GravityOnRespawn(gravity.Direction, heldDirection, HasValue);
            gravity.SetTargetDirection(direction, snap: true);
        }
    }
}
