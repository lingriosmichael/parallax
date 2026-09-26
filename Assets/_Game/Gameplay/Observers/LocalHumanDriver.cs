using Parallax.Core;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Interaction;
using Parallax.Gameplay.GravityControl;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Gameplay.Observers
{
    public sealed class LocalHumanDriver : IObserverDriver
    {
        readonly CatInputRouter router;
        ObserverContext observer;
        CatInteractor interactor;
        CatSeat seat;
        // PAX-085 (D-087): the reality's control modifiers (inverters), collected in Activate.
        IControlModifier[] modifiers = System.Array.Empty<IControlModifier>();

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
            // PAX-085 (D-087) R3: every inverter under this Observer's own reality, found once, as CatInteractor and CatSeat are.
            modifiers = observer != null && observer.Reality != null
                ? observer.Reality.GetComponentsInChildren<IControlModifier>(true)
                : System.Array.Empty<IControlModifier>();

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
            // PAX-085 (D-087): the one place input is inverted. The motor's Move only; Jump and the interactor's cmd are not.
            if (InvertsMove()) motorCommand.Move = -motorCommand.Move;
            observer.Cat.Step(motorCommand, Time.fixedDeltaTime);
            if (stand) seat.Release();
            if (interactor != null) interactor.Step(in cmd, observer);
        }

        // Inverted while any inverter says so; several never toggle each other (D-087).
        bool InvertsMove()
        {
            for (int i = 0; i < modifiers.Length; i++)
            {
                IControlModifier modifier = modifiers[i];
                if (modifier is Object unityObject && unityObject == null) continue;
                if (modifier.InvertsMove) return true;
            }
            return false;
        }

        public void Deactivate()
        {
            if (seat != null && seat.IsSeated) seat.Release();
            if (router != null) router.ResetTransientState();
        }
    }
}
