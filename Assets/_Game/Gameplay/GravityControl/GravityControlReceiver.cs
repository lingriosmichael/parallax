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
        public float LastValue { get; private set; }
        public bool HasValue { get; private set; }
        void OnEnable() => EnsureInit();
        void OnDisable() { if (transport != null) transport.ControlReceived -= OnControl; transport = null; }
        void EnsureInit()
        {
            if (root == null) root = GetComponentInParent<RealityRoot>();
            if (gravity == null) gravity = GetComponent<GravityReceiver>();
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
    }
}
