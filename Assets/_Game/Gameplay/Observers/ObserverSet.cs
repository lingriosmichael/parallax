using System;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using UnityEngine;

namespace Parallax.Gameplay.Observers
{
    public sealed class ObserverSet : MonoBehaviour
    {
        [SerializeField] ObserverContext observerA;
        [SerializeField] ObserverContext observerB;
        // PAX-054 (D-073): optional. Must implement IPauseGate (LevelPause does). Null - as in
        // Sandbox_Realities and every scene without a pause menu - changes nothing.
        [SerializeField] MonoBehaviour pauseGate;

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

            if (pauseGate != null && !(pauseGate is IPauseGate))
            {
                Debug.LogError($"ObserverSet '{gameObject.name}': pauseGate '{pauseGate.GetType().Name}' does not implement IPauseGate; ignored.", this);
            }
        }

        public bool IsPaused => pauseGate != null && pauseGate is IPauseGate gate && gate.IsPaused;

        public ObserverContext Get(ObserverId id) =>
            id == ObserverId.A ? observerA : observerB;

        void FixedUpdate()
        {
            // PAX-054 (D-073): time scale 0 already stops FixedUpdate while paused; this gate
            // makes the level tick itself honour the pause regardless of who calls it.
            if (IsPaused) return;
            Tick++;
            if (observerA != null) observerA.Step(Tick);
            if (observerB != null) observerB.Step(Tick);
            Stepped?.Invoke(Tick);
        }
    }
}
