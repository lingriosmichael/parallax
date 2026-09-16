using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Observers
{
    public sealed class InactiveDriver : IObserverDriver
    {
        ObserverContext observer;

        public InputSourceKind Kind => InputSourceKind.Inactive;

        public void Activate(ObserverContext observer)
        {
            this.observer = observer;
        }

        public void FixedTick(int tick)
        {
            if (observer == null) return;
            if (observer.Cat == null) return;

            observer.Cat.Step(CatCommand.None, Time.fixedDeltaTime);
        }

        public void Deactivate()
        {
        }
    }
}
