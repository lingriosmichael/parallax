using System;
using Parallax.Core;
using Parallax.Gameplay.Input;
using UnityEngine;

namespace Parallax.Gameplay.Observers
{
    // The only runtime code that assigns Observer drivers or enables/disables Observer cameras.
    public sealed class SoloSwitchController : MonoBehaviour
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] CatInputRouter router;

        public ObserverId Active { get; private set; }

        public event Action<ObserverId, ObserverId> Switched;

        public void Initialize(ObserverId startActive)
        {
            Active = startActive;

            ObserverContext target = observers.Get(startActive);
            ObserverContext other = observers.Get(startActive.Other());

            if (target != null) target.SetDriver(new LocalHumanDriver(router));
            if (other != null) other.SetDriver(new InactiveDriver());

            ApplyCameras(startActive);
        }

        // Called from Update, so a switch always lands between fixed ticks —
        // it never interrupts a FixedUpdate driver step already in progress.
        public void SwitchTo(ObserverId target)
        {
            if (target == Active) return;

            ObserverId from = Active;

            ObserverContext current = observers.Get(from);
            if (current != null) current.SetDriver(new InactiveDriver());

            ObserverContext next = observers.Get(target);
            if (next != null) next.SetDriver(new LocalHumanDriver(router));

            Active = target;
            ApplyCameras(target);

            Switched?.Invoke(from, target);
        }

        public void Toggle()
        {
            SwitchTo(Active.Other());
        }

        void ApplyCameras(ObserverId active)
        {
            SetCamera(active, true);
            SetCamera(active.Other(), false);
        }

        void SetCamera(ObserverId id, bool active)
        {
            ObserverContext observer = observers.Get(id);
            Camera cam = observer != null ? observer.Camera : null;
            if (cam == null) return;

            cam.enabled = active;
            if (active)
            {
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.depth = 0f;
            }
        }
    }
}
