using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>PAX-074 (D-078): a launcher that fires one arrow along a lane. The arrow is
    /// a child sprite with no collider, placed each tick from ArrowMath (ticks since the fire); while
    /// lethal, that tick's pose is tested against the cat and a hit kills through RoomDeath. No
    /// physics moves it. The launcher's authored colour is its unfired look (the host's colour when
    /// disguised); it shows honestColor from the fire tick until reset or rearm.
    /// PAX-084 (D-086): a spear is this trap with `spear` set and a shaft collider on the arrow child. It fires
    /// once, and from the tick after its stop its shaft is solid geometry the cat stands on and jumps from.
    /// PAX-099 (D-106): an arrow (never a spear) may fly at an angle from horizontal, positive towards world up; its pose
    /// and kill box turn with it, and it is harmless and non-solid once stopped.</summary>
    public sealed class ArrowTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] BoxCollider2D trigger;
        [SerializeField] SpriteRenderer launcher;
        [SerializeField] SpriteRenderer arrow;
        [SerializeField] ArrowDirection direction;
        [Tooltip("PAX-099 (D-106): degrees from horizontal, positive towards world up (0, ±30, ±45, ±60).")]
        [SerializeField] float angleDegrees;
        [Tooltip("The mouth (launcher face on the fire side, at lane height), local to this launcher.")]
        [SerializeField] Vector2 mouth;
        [SerializeField] float travel;
        [SerializeField] float arrowLength = .8f, arrowThickness = .16f, unitsPerTick = .3f;
        [SerializeField] int tellTicks = 6;
        [SerializeField] int delayTicks;
        [SerializeField] Color honestColor = new(.20f, .20f, .23f, 1f);
        [SerializeField] bool spear;
        [Tooltip("PAX-084: the spear's shaft (arrowLength x arrowThickness, non-trigger, off until it sticks). Null for an arrow.")]
        [SerializeField] BoxCollider2D shaft;

        ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8];
        int flightTicks; Color unfiredColor;

        protected override void Awake()
        {
            base.Awake();
            if (launcher == null || arrow == null || observers == null || Reality == null || (UsesOverlapSource && !IsPeriodic && trigger == null))
            { Debug.LogError($"ArrowTrap '{name}': missing launcher or arrow renderer, observers, reality, or overlap trigger.", this); enabled = false; return; }
            if (spear && shaft == null) { Debug.LogError($"ArrowTrap '{name}': a spear needs its shaft collider.", this); enabled = false; return; }
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
            flightTicks = ArrowMath.FlightTicks(travel, unitsPerTick);
            arrow.transform.localRotation = Quaternion.Euler(0f, 0f, KillAngle);
            unfiredColor = launcher.color;
            ShowUnfired();
        }

        protected override int DelayTicks => delayTicks;
        protected override CatDeathKind DeclaredDeathKind => CatDeathKind.Arrow;   // PAX-V07 §4: arrows and the spear variant

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            StepTiming(trigger != null && IsLocalHumanOverlapping(trigger, observers, filter, results, out _));
            if (!IsTimingEffectActive) return;
            int s = RoomLifeTick - LatestFireTick;
            arrow.transform.localPosition = LocalPose(s);
            arrow.enabled = true;
            launcher.color = honestColor;
            if (TryGetKillBox(out Bounds pose) && IsLocalHumanOverlapping(pose, KillAngle, observers, filter, results, out _)) { Death.Kill(Reality.Id, DeathCause.Hazard, this); return; }
            if (spear && s >= ArrowMath.StuckCheckTick(tellTicks, flightTicks) && !shaft.enabled) shaft.enabled = true;
        }

        /// <summary>The box this tick's kill test uses, or false while the arrow is harmless. The route harness names
        /// killers through this same function (PAX-084 R3). A spear adds one check on stop + 1, before its shaft
        /// switches on: the stop pose shrunk by ArrowMath.StuckShrink a side, so touching it isn't being inside it.
        /// PAX-099 (D-106): the box is unrotated; KillAngle turns it about its centre.</summary>
        public bool TryGetKillBox(out Bounds box)
        {
            box = default;
            if (!enabled || !IsTimingEffectActive) return false;
            int s = RoomLifeTick - LatestFireTick;
            bool stuckCheck = spear && s == ArrowMath.StuckCheckTick(tellTicks, flightTicks);
            if (!stuckCheck && !ArrowMath.IsLethal(s, tellTicks, flightTicks)) return false;
            float shrink = stuckCheck ? 2f * ArrowMath.StuckShrink : 0f;
            box = new Bounds(transform.TransformPoint(LocalPose(s)), new Vector3(arrowLength - shrink, arrowThickness - shrink));
            return true;
        }

        /// <summary>PAX-099 (D-106): the kill box's rotation in degrees, 0 for a level arrow.</summary>
        public float KillAngle => ArrowMath.BoxAngle(direction, angleDegrees);

        Vector2 LocalPose(int s) => angleDegrees == 0f
            ? new(ArrowMath.CentreX(mouth.x, direction, arrowLength, ArrowMath.Offset(s, tellTicks, unitsPerTick, travel)), mouth.y)
            : ArrowMath.Centre(mouth, ArrowMath.Direction(direction, angleDegrees), arrowLength, ArrowMath.Offset(s, tellTicks, unitsPerTick, travel));

        void ShowUnfired()
        {
            if (arrow == null || launcher == null) return;
            arrow.enabled = false;
            arrow.transform.localPosition = LocalPose(0);
            launcher.color = unfiredColor;
            if (shaft != null) shaft.enabled = false;
        }

        // PAX-090 (D-091): the pose, the look and the shaft all follow from the timing (the live step's rules at roomTick).
        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick)
        {
            if (!enabled || !IsTimingEffectActive) { ShowUnfired(); return; }
            int s = roomTick - LatestFireTick;
            arrow.transform.localPosition = LocalPose(s);
            arrow.enabled = true;
            launcher.color = honestColor;
            if (shaft != null) shaft.enabled = spear && s >= ArrowMath.StuckCheckTick(tellTicks, flightTicks);
        }

        protected override void OnReset() => ShowUnfired();
        protected override void OnTimingRearmed() => ShowUnfired();
    }
}
