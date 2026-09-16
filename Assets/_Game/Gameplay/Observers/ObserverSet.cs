using System;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Observers
{
    public sealed class ObserverSet : MonoBehaviour
    {
        [SerializeField] ObserverContext observerA;
        [SerializeField] ObserverContext observerB;

        public int Tick { get; private set; }

        public event Action<int> Stepped;

        void Awake()
        {
            if (observerA == null)
            {
                Debug.LogError($"ObserverSet '{gameObject.name}' has no observerA assigned.", this);
            }

            if (observerB == null)
            {
                Debug.LogWarning($"ObserverSet '{gameObject.name}' has no observerB assigned.", this);
            }
        }

        public ObserverContext Get(ObserverId id) =>
            id == ObserverId.A ? observerA : observerB;

        void FixedUpdate()
        {
            Tick++;
            if (observerA != null) observerA.Step(Tick);
            if (observerB != null) observerB.Step(Tick);
            Stepped?.Invoke(Tick);
        }
    }
}
