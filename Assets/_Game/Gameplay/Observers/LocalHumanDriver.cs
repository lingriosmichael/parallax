using Parallax.Core;
using Parallax.Gameplay.Input;
using UnityEngine;

namespace Parallax.Gameplay.Observers
{
    public sealed class LocalHumanDriver : IObserverDriver
    {
        readonly CatInputRouter router;
        ObserverContext observer;

        public LocalHumanDriver(CatInputRouter router)
        {
            this.router = router;
        }

        public InputSourceKind Kind => InputSourceKind.LocalHuman;

        public void Activate(ObserverContext observer)
        {
            this.observer = observer;

            if (router == null)
            {
                Debug.LogError("LocalHumanDriver.Activate: no CatInputRouter assigned.");
                return;
            }

            router.SetGravityFrame(observer.Gravity);
            router.ResetTransientState();
        }

        public void FixedTick(int tick)
        {
            if (router == null || observer == null) return;

            CatCommand cmd = router.Read();
            observer.Cat.Step(cmd, Time.fixedDeltaTime);
        }

        public void Deactivate()
        {
            if (router != null) router.ResetTransientState();
        }
    }
}
