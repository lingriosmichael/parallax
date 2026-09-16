using Parallax.Core;
using Parallax.Gameplay.Interaction;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Transport;
using UnityEngine;

namespace Parallax.Gameplay.Echo
{
    public enum EchoRecordState { Idle, Recording, Full, Unavailable }

    public sealed class EchoSession : MonoBehaviour
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] SoloSwitchController switchController;
        [SerializeField] TransportHost transportHost;
        [SerializeField] EchoConfig config;
        EchoRecorder recorder;
        ObserverContext recordingObserver;
        CatInteractor recordingInteractor;
        CatMotor2D recordingCat;
        Rigidbody2D recordingBody;

        public EchoRecordState State => OtherIsEchoing() ? EchoRecordState.Unavailable : recorder == null ? EchoRecordState.Idle : recorder.IsFull ? EchoRecordState.Full : EchoRecordState.Recording;
        public float RecordedSeconds => recorder == null ? 0f : recorder.FrameCount * Time.fixedDeltaTime;

        void OnEnable()
        {
            if (observers != null) observers.Stepped += OnStepped;
        }

        void OnDisable()
        {
            if (observers != null) observers.Stepped -= OnStepped;
            StopRecording(true);
        }

        public void ToggleRecord()
        {
            if (State == EchoRecordState.Unavailable) return;
            if (recorder != null)
            {
                StopRecording(true);
                return;
            }
            if (observers == null || switchController == null || config == null) return;
            recordingObserver = observers.Get(switchController.Active);
            if (recordingObserver == null || recordingObserver.Cat == null) return;
            recorder = new EchoRecorder(recordingObserver.Id, config.MaxFrames(Time.fixedDeltaTime));
            recordingCat = recordingObserver.Cat;
            recordingBody = recordingCat.GetComponent<Rigidbody2D>();
            recordingInteractor = recordingCat.GetComponent<CatInteractor>();
            if (recordingInteractor != null) recordingInteractor.Requested += OnRequested;
        }

        public bool TryCreateReplayDriver(ObserverId id, out IObserverDriver driver)
        {
            driver = null;
            if (recorder == null || recordingObserver == null || recordingObserver.Id != id) return false;
            EchoRecording recording = recorder.Finish();
            StopRecording(true);
            if (recording.Frames.Count == 0) return false;
            if (transportHost == null || transportHost.Transport == null || transportHost.Sequencer == null)
            {
                Debug.LogWarning($"EchoSession '{gameObject.name}' cannot create replay: TransportHost, transport, or sequencer is missing.", this);
                return false;
            }
            driver = new EchoReplayDriver(new EchoPlayback(recording), new AnchorRequester(transportHost.Transport, transportHost.Sequencer, EventOrigins.Echo(id)));
            return true;
        }

        void OnStepped(int tick)
        {
            if (recorder == null || recorder.IsFull || recordingObserver == null || recordingCat == null || recordingBody == null || recordingObserver.Reality == null) return;
            var frame = new EchoFrame
            {
                Position = recordingObserver.Reality.ToLocal(recordingBody.position),
                Rotation = recordingBody.rotation,
                GravityDirection = recordingObserver.Gravity != null ? recordingObserver.Gravity.Direction : Vector2.down,
                FacingRight = recordingCat.FacingRight,
            };
            recorder.AddFrame(frame);
            if (!recorder.IsFull) return;
            Unsubscribe();
        }

        void OnRequested(AnchorId anchor, float targetValue)
        {
            if (recorder != null) recorder.AddAnchorEvent(anchor, targetValue);
        }

        void StopRecording(bool discard)
        {
            Unsubscribe();
            if (discard) recorder = null;
            recordingObserver = null;
            recordingCat = null;
            recordingBody = null;
        }

        void Unsubscribe()
        {
            if (recordingInteractor != null) recordingInteractor.Requested -= OnRequested;
            recordingInteractor = null;
        }

        bool OtherIsEchoing()
        {
            if (observers == null || switchController == null) return false;
            ObserverContext other = observers.Get(switchController.Active.Other());
            return other != null && other.Driver is EchoReplayDriver;
        }
    }
}
