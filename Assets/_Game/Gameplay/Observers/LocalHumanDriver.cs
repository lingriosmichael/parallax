using Parallax.Core;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Interaction;
using Parallax.Gameplay.GravityControl;
using UnityEngine;

namespace Parallax.Gameplay.Observers
{
    public sealed class LocalHumanDriver : IObserverDriver
    {
        readonly CatInputRouter router;
        ObserverContext observer;
        CatInteractor interactor;
        CatSeat seat;

        public LocalHumanDriver(CatInputRouter router)
        {
            this.router = router;
        }

        public InputSourceKind Kind => InputSourceKind.LocalHuman;

        public void Activate(ObserverContext observer)
        {
            this.observer = observer;
            interactor = observer != null && observer.Cat != null ? observer.Cat.GetComponent<CatInteractor>() : null;
            seat = observer != null && observer.Cat != null ? observer.Cat.GetComponent<CatSeat>() : null;

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
            bool seated = seat != null && seat.IsSeated;
            bool stand = SeatInteractRouting.Route(ref cmd, seated);
            CatCommand motorCommand = SeatCommandFilter.Apply(cmd, seated);
            observer.Cat.Step(motorCommand, Time.fixedDeltaTime);
            if (stand) seat.Release();
            if (interactor != null) interactor.Step(in cmd, observer);
        }

        public void Deactivate()
        {
            if (seat != null && seat.IsSeated) seat.Release();
            if (router != null) router.ResetTransientState();
        }
    }
}
