using Parallax.Core;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Transport;
using Parallax.Gameplay.Echo;
using Parallax.Gameplay.GravityControl;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Parallax.DebugTools
{
    // Dev-only. Never assigns drivers — only reads Observer state and toggles the
    // inactive Observer's Camera component for picture-in-picture.
    public sealed class DebugPanel : MonoBehaviour, ITouchReservedRegion
    {
        static readonly AnchorId DebugAnchorId = new AnchorId(65535);

        [SerializeField] ObserverSet observers;
        [SerializeField] SoloSwitchController switchController;
        [SerializeField] LocalTransportHost transportHost;
        [SerializeField] EchoSession echoSession;
        [SerializeField] CatSeat seatA;
        [SerializeField] CatSeat seatB;
        [SerializeField] GravityControlReceiver receiverA;
        [SerializeField] GravityControlReceiver receiverB;

        static readonly Rect DbgButtonRect = new Rect(10f, 10f, 70f, 30f);
        static readonly Rect PanelRect = new Rect(10f, 45f, 340f, 390f);

        bool open;
        bool pipOn;
        bool debugAnchorRegistered;
        float lastRequestedDebugValue;

        float fpsTimer;
        int fpsFrames;
        float fps;

        void OnEnable()
        {
            if (switchController != null) switchController.Switched += OnSwitched;
        }

        void OnDisable()
        {
            if (switchController != null) switchController.Switched -= OnSwitched;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.backquoteKey.wasPressedThisFrame)
            {
                open = !open;
            }

            fpsTimer += Time.unscaledDeltaTime;
            fpsFrames++;
            if (fpsTimer >= 0.5f)
            {
                fps = fpsFrames / fpsTimer;
                fpsTimer = 0f;
                fpsFrames = 0;
            }
        }

        void OnSwitched(ObserverId from, ObserverId to)
        {
            ApplyPiP();
        }

        void ApplyPiP()
        {
            if (observers == null || switchController == null) return;

            ObserverContext inactive = observers.Get(switchController.Active.Other());
            Camera cam = inactive != null ? inactive.Camera : null;
            if (cam == null) return;

            cam.enabled = pipOn;
            if (pipOn)
            {
                cam.rect = new Rect(0.70f, 0.70f, 0.28f, 0.28f);
                cam.depth = 1f;
            }
        }

        void TogglePiP()
        {
            pipOn = !pipOn;
            ApplyPiP();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (GUI.Button(DbgButtonRect, "DBG"))
            {
                open = !open;
            }

            if (!open || observers == null || switchController == null) return;

            GUILayout.BeginArea(PanelRect, GUI.skin.box);

            GUILayout.Label($"Active: {switchController.Active}   Tick: {observers.Tick}   FPS: {fps:F0}");

            DrawObserverRow(ObserverId.A);
            DrawObserverRow(ObserverId.B);
            DrawControlRow(ObserverId.A, seatA, receiverA);
            DrawControlRow(ObserverId.B, seatB, receiverB);

            bool nextPiP = GUILayout.Toggle(pipOn, "PiP (inactive reality, top-right)");
            if (nextPiP != pipOn)
            {
                TogglePiP();
            }

            DrawTransport();
            DrawEcho();

            GUILayout.EndArea();
        }

        static void DrawControlRow(ObserverId id, CatSeat seat, GravityControlReceiver receiver)
        {
            string seated = seat != null && seat.IsSeated ? "seated" : "-";
            string received = receiver != null && receiver.HasValue ? receiver.LastValue.ToString("F2") : "-";
            GUILayout.Label($"Seat {id}: {seated}  Recv {id}: {received}");
        }

        void DrawEcho()
        {
            if (echoSession != null) GUILayout.Label($"Echo: {echoSession.State} {echoSession.RecordedSeconds:F1}s");
            DrawReplayRow(ObserverId.A);
            DrawReplayRow(ObserverId.B);
        }

        void DrawReplayRow(ObserverId id)
        {
            ObserverContext observer = observers.Get(id);
            if (observer == null || !(observer.Driver is EchoReplayDriver replay)) return;
            GUILayout.Label($"Replay {id}: {replay.Cursor}/{replay.FrameCount}{(replay.IsHolding ? " [holding]" : "")}");
        }

        void DrawTransport()
        {
            if (transportHost == null || transportHost.Local == null)
            {
                GUILayout.Label("Transport: (missing)");
                return;
            }

            LocalTransport transport = transportHost.Local;

            float ticksEquivalent = Mathf.Max(1f, Mathf.Ceil(transport.LatencyMs / (Time.fixedDeltaTime * 1000f)));
            GUILayout.Label($"Latency: {transport.LatencyMs:F0} ms (~{ticksEquivalent:F0} ticks)");
            float nextLatency = GUILayout.HorizontalSlider(transport.LatencyMs, 0f, 400f);
            if (!Mathf.Approximately(nextLatency, transport.LatencyMs))
            {
                transport.LatencyMs = nextLatency;
            }

            GUILayout.Label($"PendingCount: {transport.PendingCount}   LastCommitResult: {transport.LastCommitResult}");

            if (GUILayout.Button("Debug anchor 0<->1"))
            {
                PressDebugAnchorButton(transportHost);
            }

            GUILayout.Label("Anchors:");
            foreach (var entry in transportHost.Registry.All)
            {
                GUILayout.Label($"  {entry.Key}: value {entry.Value.Value:F2}  rev {entry.Value.Revision}");
            }
        }

        void PressDebugAnchorButton(LocalTransportHost host)
        {
            if (!debugAnchorRegistered)
            {
                debugAnchorRegistered = true;
                host.Registry.Register(DebugAnchorId, 0f);
                lastRequestedDebugValue = 0f;
            }

            // Toggle against the last value we requested, not the committed value: a second
            // press before the first request's artificial delivery delay elapses must still
            // target the opposite value, or it would collapse to a NoChange on commit.
            lastRequestedDebugValue = lastRequestedDebugValue == 0f ? 1f : 0f;

            var request = new AnchorRequest(DebugAnchorId, lastRequestedDebugValue, EventOrigin.System, host.Sequencer.Next(EventOrigin.System));
            host.Transport.RequestAnchor(request);
        }

        void DrawObserverRow(ObserverId id)
        {
            ObserverContext observer = observers.Get(id);
            if (observer == null)
            {
                GUILayout.Label($"{id}: (missing)");
                return;
            }

            string kind = observer.Driver != null ? observer.Driver.Kind.ToString() : "none";
            GravityReceiverSummary(observer, out float angleDeg, out bool grounded, out float speed);

            GUILayout.Label($"{id}: {kind}  grav {angleDeg:F0}°  grounded {grounded}  speed {speed:F1}");
        }

        static void GravityReceiverSummary(ObserverContext observer, out float angleDeg, out bool grounded, out float speed)
        {
            angleDeg = 0f;
            grounded = false;
            speed = 0f;

            if (observer.Gravity != null)
            {
                angleDeg = Vector2.SignedAngle(Vector2.down, observer.Gravity.Direction);
            }

            if (observer.Cat != null)
            {
                grounded = observer.Cat.IsGrounded;

                var body = observer.Cat.GetComponent<Rigidbody2D>();
                if (body != null) speed = body.linearVelocity.magnitude;
            }
        }
#endif

        public bool ContainsScreenPoint(Vector2 screenPos)
        {
            Vector2 guiPoint = new Vector2(screenPos.x, Screen.height - screenPos.y);

            if (DbgButtonRect.Contains(guiPoint)) return true;
            if (open && PanelRect.Contains(guiPoint)) return true;

            return false;
        }
    }
}
