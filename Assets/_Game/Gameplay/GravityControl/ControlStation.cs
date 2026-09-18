using Parallax.Core;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Interaction;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.GravityControl
{
    public sealed class ControlStation : MonoBehaviour, IInteractable
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] MonoBehaviour inputSource;
        [SerializeField] DialGravityInput dial;
        [SerializeField] SpriteRenderer glow;
        [SerializeField] SpriteRenderer transitPulse;
        IGravityControlInput input;
        ObserverId occupant;
        readonly SeatState seatState = new SeatState();
        float lastPublished;
        bool hasPublished;
        float pulseUntil;
        void Awake()
        {
            input = inputSource as IGravityControlInput;
            if (input == null) Debug.LogError($"ControlStation '{name}' inputSource must implement IGravityControlInput.", this);
            seatState.Released += OnSeatReleased;
        }
        void OnEnable() { if (observers != null) observers.Stepped += OnStepped; }
        void OnDisable()
        {
            Release();
            if (observers != null) observers.Stepped -= OnStepped;
        }
        void OnDestroy()
        {
            Release();
            seatState.Released -= OnSeatReleased;
        }
        public void Interact(ObserverId observerId, IAnchorRequester requester)
        {
            ObserverContext context = observers != null ? observers.Get(observerId) : null;
            CatSeat nextSeat = context != null && context.Cat != null ? context.Cat.GetComponent<CatSeat>() : null;
            if (nextSeat == null || input == null) return;
            if (ReferenceEquals(nextSeat, seatState.Occupant)) { nextSeat.Release(); return; }
            if (seatState.IsOccupied || nextSeat.IsSeated || !seatState.TryOccupy(nextSeat, this)) return;
            occupant = observerId;
            nextSeat.SitAt(seatState);
            input.Calibrate();
            lastPublished = 0f;
            hasPublished = false;
            if (dial != null) dial.SetVisible(true);
            SetGlow(0f);
        }
        public void Release()
        {
            seatState.Release();
        }

        void OnSeatReleased()
        {
            if (dial != null) dial.SetVisible(false);
            SetGlow(0.3f);
        }
        void OnStepped(int tick)
        {
            if (!seatState.IsOccupied || observers == null || input == null) return;
            ObserverContext context = observers.Get(occupant);
            if (context == null || context.Driver == null || context.Driver.Kind != InputSourceKind.LocalHuman) return;
            float value = input.ReadNormalized();
            SetGlow(0.3f + 0.7f * Mathf.Abs(value));
            if ((!hasPublished && Mathf.Approximately(value, 0f)) || (hasPublished && Mathf.Abs(value - lastPublished) <= 0.001f)) return;
            CatInteractor interactor = context.Cat != null ? context.Cat.GetComponent<CatInteractor>() : null;
            if (interactor == null) return;
            interactor.PublishControl(ControlChannel.GravityAngle, occupant, value);
            lastPublished = value;
            hasPublished = true;
            pulseUntil = Time.time + 0.25f;
        }
        void Update()
        {
            if (transitPulse == null) return;
            Color color = new Color(1f, 1f, 1f, Time.time < pulseUntil ? 1f : 0f);
            if (transitPulse.color != color) transitPulse.color = color;
        }

        void SetGlow(float alpha)
        {
            if (glow == null) return;
            Color color = new Color(1f, 1f, 1f, alpha);
            if (glow.color != color) glow.color = color;
        }
    }
}
