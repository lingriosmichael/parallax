using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>PAX-120 (D-197): a portal pair. A LocalHuman cat whose centre enters the entry box is moved by (exit centre - entry
    /// centre), keeping its velocity (CatMotor2D.ApplyPush); it can't go through again until it has left both boxes (no ping-pong).
    /// TwoWay makes the exit an entry too. The exit may move along a path (smooth or glitch, on the trap's Periodic timing), so the
    /// pair is a function of ticks plus one flag (armed, by value in the snapshot): a reset and a rewind restore it exactly (D-091).
    /// The exit is the "Exit" child; its look flickers through a glitch warning.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class PortalTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] Transform exit;
        [SerializeField] Vector2 exitHome, exitSize = Vector2.one;
        [SerializeField] bool twoWay;
        [Header("A moving exit (off with no points)")]
        [SerializeField] Vector2[] exitPath;
        [SerializeField] bool exitGlitch;
        [SerializeField] int moveTicks = 1, holdTicks, returnTicks, warnTicks;
        [SerializeField] SpriteRenderer exitLook;
        BoxCollider2D entry; Color exitColor;
        bool armed = true;

        public bool Armed => armed;

        protected override void Awake()
        {
            base.Awake(); entry = GetComponent<BoxCollider2D>();
            if (entry == null || observers == null || Reality == null || exit == null) { Debug.LogError($"PortalTrap '{name}': missing entry box, observers, reality or exit.", this); enabled = false; return; }
            if (exitLook != null) exitColor = exitLook.color;
            Place(-1);
        }

        bool Moves => exitPath != null && exitPath.Length > 0;

        // The exit's offset from its home `since` ticks after the timing's fire (none at rest or unfired).
        Vector2 ExitOffset(int since) => Moves && since >= 0 ? TrapPath.Offset(exitPath, exitGlitch, since, moveTicks, holdTicks, returnTicks) : Vector2.zero;

        void Place(int since)
        {
            exit.localPosition = new Vector3(exitHome.x + ExitOffset(since).x, exitHome.y + ExitOffset(since).y, exit.localPosition.z);
            if (exitLook == null) return;
            bool warning = Moves && exitGlitch && since >= 0 && TrapPath.GlitchWarning(exitPath, since, moveTicks, holdTicks, returnTicks, warnTicks, out _);
            Color c = exitColor; if (warning && since % 2 == 1) c.a *= .45f; exitLook.color = c;
        }

        Bounds ExitBounds => new(exit.position, exitSize);

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            if (Moves) StepTiming(false);
            Place(IsTimingEffectActive ? RoomLifeTick - LatestFireTick : -1);
            if (!TryGetLocalHumanColliderBounds(observers, out Bounds cat)) return;
            Bounds inBox = entry.bounds, outBox = ExitBounds;
            if (!armed)
            {
                if (!cat.Intersects(inBox) && !cat.Intersects(outBox)) armed = true;
                return;
            }
            Vector2 centre = cat.center;
            if (Contains(inBox, centre)) Go((Vector2)(outBox.center - inBox.center));
            else if (twoWay && Contains(outBox, centre)) Go((Vector2)(inBox.center - outBox.center));
        }

        static bool Contains(Bounds b, Vector2 p) => p.x >= b.min.x && p.x <= b.max.x && p.y >= b.min.y && p.y <= b.max.y;

        void Go(Vector2 delta)
        {
            observers.Get(Reality.Id).Cat.ApplyPush(delta);
            armed = false;
        }

        protected override void CaptureExtra(ref TrapSnapshot snapshot) => snapshot.ExtraInt = armed ? 1 : 0;
        protected override void OnReset() { armed = true; Place(-1); }
        protected override void OnTimingRearmed() => Place(-1);
        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick) { armed = snapshot.ExtraInt == 1; Place(IsTimingEffectActive ? roomTick - LatestFireTick : -1); }
    }
}
