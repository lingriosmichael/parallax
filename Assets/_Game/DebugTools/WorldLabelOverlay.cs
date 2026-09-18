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
            public AnchorId Anchor;
            public System.Func<string> Text;
        }

        [SerializeField] ObserverSet observers;
        [SerializeField] SoloSwitchController switchController;
        [SerializeField] TransportHost transportHost;
        [SerializeField] DebugPanel debugPanel;

        readonly List<Entry> entries = new List<Entry>();
        bool visible = true;

        void OnEnable()
        {
            if (switchController != null) switchController.Switched += OnSwitched;
            Rescan();
        }

        void OnDisable()
        {
            if (switchController != null) switchController.Switched -= OnSwitched;
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) visible = !visible;
        }

        void OnSwitched(ObserverId from, ObserverId to) => Rescan();

        void Rescan()
        {
            entries.Clear();
            foreach (RealityPresenter presenter in FindObjectsByType<RealityPresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (presenter.Definition == null) continue;
                Tag(presenter.Definition.Id);
                foreach (RealityManifestation manifestation in presenter.Manifestations)
                    if (manifestation != null) AddAnchor(manifestation, presenter.Definition.Id, manifestation.name.Contains("Gate"));
            }
            foreach (VineInteractable vine in FindObjectsByType<VineInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (vine.Definition != null) AddAnchor(vine, vine.Definition.Id, false);
            foreach (PressurePlateSensor plate in FindObjectsByType<PressurePlateSensor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (plate.Definition != null) AddPlate(plate, plate.Definition.Id);
            foreach (ControlStation station in FindObjectsByType<ControlStation>(FindObjectsInactive.Include, FindObjectsSortMode.None)) AddStation(station);
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
            if (observer == null || observer.Cat == null) return;
            entries.Add(new Entry { Target = observer.Cat, Text = () =>
            {
                CatSeat seat = observer.Cat.GetComponent<CatSeat>();
                EchoReplayDriver echo = observer.Driver as EchoReplayDriver;
                return LabelFormatter.Cat(observer.Id, observer.Driver?.Kind ?? InputSourceKind.Inactive, seat != null && seat.IsSeated,
                    echo == null ? 0f : echo.Cursor * Time.fixedDeltaTime, echo != null && echo.IsHolding);
            }});
        }

        void AddAnchor(Component target, AnchorId anchor, bool gate)
        {
            Tag(anchor);
            entries.Add(new Entry { Target = target, Anchor = anchor, Text = () => gate ? LabelFormatter.Gate(Tag(anchor), Value(anchor)) : LabelFormatter.Anchor(Tag(anchor), PendingOrValue(anchor, out bool pending), pending) });
        }

        void AddPlate(PressurePlateSensor plate, AnchorId anchor)
        {
            Tag(anchor);
            entries.Add(new Entry { Target = plate, Anchor = anchor, Text = () => LabelFormatter.Plate(Tag(anchor), plate.IsPressed, plate.IsPressedByEcho) });
        }

        void AddStation(ControlStation station) => entries.Add(new Entry { Target = station, Text = () => LabelFormatter.Station(station.IsOccupied, station.Occupant, station.CurrentValue) });

        string Tag(AnchorId anchor)
        {
            return LabelFormatter.AnchorTag(anchor);
        }

        float Value(AnchorId anchor) => transportHost != null && transportHost.Registry != null && transportHost.Registry.TryGet(anchor, out AnchorState state) ? state.Value : 0f;
        float PendingOrValue(AnchorId anchor, out bool pending)
        {
            LocalTransport local = (transportHost as LocalTransportHost)?.Local;
            float target = 0f;
            pending = local != null && local.TryGetPendingAnchorTarget(anchor, out target);
            return pending ? target : Value(anchor);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (!visible || (debugPanel != null && !debugPanel.LabelsVisible) || switchController == null || observers == null) return;
            ObserverContext active = observers.Get(switchController.Active);
            if (active == null || active.Camera == null) return;
            Camera camera = active.Camera;
            foreach (Entry entry in entries)
            {
                if (entry.Target == null || !InActiveReality(entry.Target, active.Id)) continue;
                Vector3 point = camera.WorldToScreenPoint(entry.Target.transform.position + Vector3.up * 0.8f);
                if (point.z < 0f || !camera.pixelRect.Contains((Vector2)point)) continue;
                GUI.Box(new Rect(point.x, Screen.height - point.y - 22f, 180f, 22f), entry.Text());
            }
        }
#endif

        static bool InActiveReality(Component component, ObserverId active) => component.GetComponentInParent<RealityRoot>()?.Id == active;
    }
}
