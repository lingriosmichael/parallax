using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Transport
{
    public sealed class LocalTransportHost : MonoBehaviour
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] int initialLatencyMs = 0;

        public AnchorRegistry Registry { get; private set; }
        public IRealityTransport Transport => Local;
        public LocalTransport Local { get; private set; }

        void Awake()
        {
            if (observers == null)
            {
                Debug.LogError($"LocalTransportHost '{gameObject.name}' has no ObserverSet assigned.", this);
                return;
            }

            Registry = new AnchorRegistry();
            Local = new LocalTransport(Registry, Time.fixedDeltaTime, () => observers.Tick) { LatencyMs = initialLatencyMs };
        }

        void OnEnable()
        {
            if (observers != null) observers.Stepped += OnStepped;
        }

        void OnDisable()
        {
            if (observers != null) observers.Stepped -= OnStepped;
        }

        void OnStepped(int tick)
        {
            Local.Pump(tick);
        }
    }
}
