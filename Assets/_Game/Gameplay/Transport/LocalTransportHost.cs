using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Transport
{
    public sealed class LocalTransportHost : TransportHost
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] int initialLatencyMs = 0;

        AnchorRegistry registry;
        EventSequencer sequencer;
        bool warnedNoObservers;

        public override AnchorRegistry Registry { get { EnsureCreated(); return registry; } }
        public override IRealityTransport Transport { get { EnsureCreated(); return Local; } }
        public override EventSequencer Sequencer { get { EnsureCreated(); return sequencer; } }
        public LocalTransport Local { get; private set; }

        void Awake()
        {
            EnsureCreated();
        }

        void EnsureCreated()
        {
            if (Local != null) return;
            if (observers == null)
            {
                if (!warnedNoObservers)
                {
                    Debug.LogError($"LocalTransportHost '{gameObject.name}' has no ObserverSet assigned.", this);
                    warnedNoObservers = true;
                }
                return;
            }

            registry = new AnchorRegistry();
            sequencer = new EventSequencer();
            Local = new LocalTransport(registry, Time.fixedDeltaTime, () => observers.Tick) { LatencyMs = initialLatencyMs };
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
            if (Local != null) Local.Pump(tick);
        }
    }
}
