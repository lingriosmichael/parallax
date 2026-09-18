using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Anchors;
using Parallax.Gameplay.Echo;
using Parallax.Gameplay.GravityControl;
using Parallax.Gameplay.Interaction;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Sensors;
using Parallax.Gameplay.Transport;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Parallax.DebugTools
{
    public sealed class WorldLabelOverlay : MonoBehaviour
    {
        sealed class Entry
        {
            public Component Target;
            public ObserverId Reality;
            public CatSeat Seat;
            public System.Func<string> Text;
        }

        readonly struct AnchorReality : System.IEquatable<AnchorReality>
        {
            readonly AnchorId anchor;
            readonly ObserverId reality;

            public AnchorReality(AnchorId anchor, ObserverId reality)
            {
                this.anchor = anchor;
                this.reality = reality;
            }

            public bool Equals(AnchorReality other) => anchor == other.anchor && reality == other.reality;
            public override bool Equals(object obj) => obj is AnchorReality other && Equals(other);
            public override int GetHashCode() => (anchor.GetHashCode() * 397) ^ (int)reality;
        }

        [SerializeField] ObserverSet observers;
        [SerializeField] SoloSwitchController switchController;
        [SerializeField] TransportHost transportHost;
        [SerializeField] DebugPanel debugPanel;

        readonly List<Entry> entries = new List<Entry>();

        void OnEnable()
        {
            Rescan();
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame && debugPanel != null) debugPanel.ToggleLabels();
        }

        void Rescan()
        {
            entries.Clear();
            var touchLabels = new HashSet<AnchorReality>();
            var pressureWrittenAnchors = new HashSet<AnchorId>();
            foreach (VineInteractable vine in FindObjectsByType<VineInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (vine.Definition == null || !TryGetReality(vine, out ObserverId reality)) continue;
                touchLabels.Add(new AnchorReality(vine.Definition.Id, reality));
                AddAnchor(vine, reality, vine.Definition.Id, false);
            }
            foreach (PressurePlateSensor plate in FindObjectsByType<PressurePlateSensor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (plate.Definition == null || !TryGetReality(plate, out ObserverId reality)) continue;
                touchLabels.Add(new AnchorReality(plate.Definition.Id, reality));
                pressureWrittenAnchors.Add(plate.Definition.Id);
                AddPlate(plate, reality, plate.Definition.Id);
            }
            foreach (RealityPresenter presenter in FindObjectsByType<RealityPresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (presenter.Definition == null) continue;
                foreach (RealityManifestation manifestation in presenter.Manifestations)
                {
                    if (manifestation == null || !TryGetReality(manifestation, out ObserverId reality)) continue;
                    AnchorId anchor = presenter.Definition.Id;
                    if (!touchLabels.Contains(new AnchorReality(anchor, reality)))
                        AddAnchor(manifestation, reality, anchor, pressureWrittenAnchors.Contains(anchor));
                }
            }
            foreach (ControlStation station in FindObjectsByType<ControlStation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (TryGetReality(station, out ObserverId reality)) AddStation(station, reality);
            AddCats();
        }

        void AddCats()
        {
            if (observers == null) return;
            AddCat(observers.Get(ObserverId.A));
            AddCat(observers.Get(ObserverId.B));
        }

        void AddCat(ObserverContext observer)
        {
            if (observer == null || observer.Cat == null || !TryGetReality(observer.Cat, out ObserverId reality)) return;
            CatSeat seat = observer.Cat.GetComponent<CatSeat>();
            var entry = new Entry { Target = observer.Cat, Reality = reality, Seat = seat };
            entry.Text = () =>
            {
                IObserverDriver driver = observer.Driver;
                EchoReplayDriver echo = driver as EchoReplayDriver;
                InputSourceKind kind = driver != null ? driver.Kind : InputSourceKind.Inactive;
                return LabelFormatter.Cat(observer.Id, kind, entry.Seat != null && entry.Seat.IsSeated,
                    echo == null ? 0f : echo.Cursor * Time.fixedDeltaTime, echo != null && echo.IsHolding);
            };
            entries.Add(entry);
        }

        void AddAnchor(Component target, ObserverId reality, AnchorId anchor, bool gate)
        {
            string tag = LabelFormatter.AnchorTag(anchor);
            entries.Add(new Entry { Target = target, Reality = reality, Text = () => gate ? LabelFormatter.Gate(tag, Value(anchor)) : LabelFormatter.Anchor(tag, PendingOrValue(anchor, out bool pending), pending) });
        }

        void AddPlate(PressurePlateSensor plate, ObserverId reality, AnchorId anchor)
        {
            string tag = LabelFormatter.AnchorTag(anchor);
            entries.Add(new Entry { Target = plate, Reality = reality, Text = () => LabelFormatter.Plate(tag, plate.IsPressed, plate.IsPressedByEcho) });
        }

        void AddStation(ControlStation station, ObserverId reality) => entries.Add(new Entry { Target = station, Reality = reality, Text = () => LabelFormatter.Station(station.IsOccupied, station.Occupant, station.CurrentValue) });

        float Value(AnchorId anchor) => transportHost != null && transportHost.Registry != null && transportHost.Registry.TryGet(anchor, out AnchorState state) ? state.Value : 0f;
        float PendingOrValue(AnchorId anchor, out bool pending)
        {
            LocalTransportHost host = transportHost as LocalTransportHost;
            LocalTransport local = host != null ? host.Local : null;
            float target = 0f;
            pending = local != null && local.TryGetPendingAnchorTarget(anchor, out target);
            return pending ? target : Value(anchor);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || debugPanel == null || !debugPanel.LabelsVisible || switchController == null || observers == null) return;
            ObserverContext active = observers.Get(switchController.Active);
            if (active == null || active.Camera == null) return;
            Camera camera = active.Camera;
            foreach (Entry entry in entries)
            {
                if (entry.Target == null || entry.Reality != active.Id) continue;
                Vector3 point = camera.WorldToScreenPoint(entry.Target.transform.position + Vector3.up * 0.8f);
                if (point.z < 0f || !camera.pixelRect.Contains((Vector2)point)) continue;
                GUI.Box(new Rect(point.x, Screen.height - point.y - 22f, 180f, 22f), entry.Text());
            }
        }
#endif

        static bool TryGetReality(Component component, out ObserverId reality)
        {
            RealityRoot root = component.GetComponentInParent<RealityRoot>();
            if (root == null)
            {
                reality = default;
                return false;
            }

            reality = root.Id;
            return true;
        }
    }
}
