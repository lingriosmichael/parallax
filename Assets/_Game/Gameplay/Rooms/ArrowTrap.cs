using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>PAX-074 (D-078): a launcher that fires one arrow along a horizontal lane. The arrow is
    /// a child sprite with no collider, placed each tick from ArrowMath (ticks since the fire); while
    /// lethal, that tick's pose is tested against the cat and a hit kills through RoomDeath. No
    /// physics moves it. The launcher's authored colour is its unfired look (the host's colour when
    /// disguised); it shows honestColor from the fire tick until reset or rearm.</summary>
    public sealed class ArrowTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] BoxCollider2D trigger;
        [SerializeField] SpriteRenderer launcher;
        [SerializeField] SpriteRenderer arrow;
        [SerializeField] ArrowDirection direction;
        [Tooltip("The mouth (launcher face on the fire side, at lane height), local to this launcher.")]
        [SerializeField] Vector2 mouth;
        [SerializeField] float travel;
        [SerializeField] float arrowLength = .8f, arrowThickness = .16f, unitsPerTick = .3f;
        [SerializeField] int tellTicks = 6;
        [SerializeField] int delayTicks;
        [SerializeField] Color honestColor = new(.20f, .20f, .23f, 1f);

        ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8];
        int flightTicks; Color unfiredColor;

        protected override void Awake()
        {
            base.Awake();
            if (launcher == null || arrow == null || observers == null || Reality == null || (UsesOverlapSource && !IsPeriodic && trigger == null))
            { Debug.LogError($"ArrowTrap '{name}': missing launcher or arrow renderer, observers, reality, or overlap trigger.", this); enabled = false; return; }
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
            flightTicks = ArrowMath.FlightTicks(travel, unitsPerTick);
            unfiredColor = launcher.color;
            ShowUnfired();
        }

        protected override int DelayTicks => delayTicks;

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            StepTiming(trigger != null && IsLocalHumanOverlapping(trigger, observers, filter, results, out _));
            if (!IsTimingEffectActive) return;
            int s = RoomLifeTick - LatestFireTick;
            arrow.transform.localPosition = LocalPose(s);
            arrow.enabled = true;
            launcher.color = honestColor;
            if (!ArrowMath.IsLethal(s, tellTicks, flightTicks)) return;
            Bounds pose = new(arrow.transform.position, new Vector3(arrowLength, arrowThickness));
            if (IsLocalHumanOverlapping(pose, observers, filter, results, out _)) Death.Kill(Reality.Id, DeathCause.Hazard);
        }

        Vector2 LocalPose(int s) =>
            new(ArrowMath.CentreX(mouth.x, direction, arrowLength, ArrowMath.Offset(s, tellTicks, unitsPerTick, travel)), mouth.y);

        void ShowUnfired()
        {
            if (arrow == null || launcher == null) return;
            arrow.enabled = false;
            arrow.transform.localPosition = LocalPose(0);
            launcher.color = unfiredColor;
        }

        protected override void OnReset() => ShowUnfired();
        protected override void OnTimingRearmed() => ShowUnfired();
    }
}
