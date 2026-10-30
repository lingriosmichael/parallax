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
    /// and kill box turn with it, and it is harmless and non-solid once stopped.
    /// PAX-107 (D-123): with mirrorWithGravity, each shot takes the lane the cat's gravity picks as it fires: the authored one
    /// with gravity down, the launcher moved up by mirrorShift (its mirror across the room) with gravity up. A shot in flight
    /// keeps its lane.
    /// PAX-111 (D-177): with `beam` set it is a laser turret: no arrow flies. The `arrow` child is the whole lane (beamLength
    /// along it, from the mouth); through the tell only `ray`, a thin line over the same lane, shows (harmless); then the lane
    /// is lethal and drawn for beamTicks; then both are dark until the next fire. A spear with `ray` set (the hard-light bar)
    /// shows the ray through its tell and flies as before.</summary>
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
        [Tooltip("PAX-107 (D-123): each shot flies on the mirrored lane while the cat's gravity is up.")]
        [SerializeField] bool mirrorWithGravity;
        [Tooltip("PAX-107 (D-123): how far up the launcher moves for a mirrored shot (local units).")]
        [SerializeField] float mirrorShift;
        [Tooltip("PAX-111 (D-177): a laser beam over the whole lane instead of a flying arrow.")]
        [SerializeField] bool beam;
        [SerializeField] int beamTicks;
        [Tooltip("PAX-111 (D-177): the beam's length along the lane, from the mouth (the arrow child is placed at its middle).")]
        [SerializeField] float beamLength;
        [Tooltip("PAX-111 (D-177): the thin warning ray over the lane, shown through the tell (a beam, or a hard-light bar). Null for none.")]
        [SerializeField] SpriteRenderer ray;

        ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8];
        int flightTicks; Color unfiredColor;
        Vector3 home; bool mirrored;

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
            home = transform.localPosition;
            ShowUnfired();
        }

        protected override int DelayTicks => delayTicks;
        protected override CatDeathKind DeclaredDeathKind => CatDeathKind.Arrow;   // PAX-V07 §4: arrows and the spear variant

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            if (StepTiming(trigger != null && IsLocalHumanOverlapping(trigger, observers, filter, results, out _)) && mirrorWithGravity) SetLane(CatGravityUp());
            if (!IsTimingEffectActive) return;
            int s = RoomLifeTick - LatestFireTick;
            ShowShot(s);
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
            if (beam)
            {
                if (!ArrowMath.IsBeamLethal(s, tellTicks, beamTicks)) return false;
                box = new Bounds(arrow.transform.position, new Vector3(beamLength, arrowThickness));
                return true;
            }
            if (!stuckCheck && !ArrowMath.IsLethal(s, tellTicks, flightTicks)) return false;
            float shrink = stuckCheck ? 2f * ArrowMath.StuckShrink : 0f;
            box = new Bounds(transform.TransformPoint(LocalPose(s)), new Vector3(arrowLength - shrink, arrowThickness - shrink));
            return true;
        }

        /// <summary>PAX-099 (D-106): the kill box's rotation in degrees, 0 for a level arrow.</summary>
        public float KillAngle => ArrowMath.BoxAngle(direction, angleDegrees);

        // PAX-111 (D-177): this tick's look from s (ticks since the fire): a beam's ray then lane, or the flying arrow (and a
        // hard-light bar's ray through its tell).
        void ShowShot(int s)
        {
            launcher.color = honestColor;
            if (beam)
            {
                ArrowBeamPhase phase = ArrowMath.BeamPhase(s, tellTicks, beamTicks);
                if (ray != null) ray.enabled = phase == ArrowBeamPhase.Tell;
                arrow.enabled = phase == ArrowBeamPhase.Beam;
                return;
            }
            if (ray != null) ray.enabled = ArrowMath.IsTell(s, tellTicks);
            arrow.transform.localPosition = LocalPose(s);
            arrow.enabled = true;
        }

        // A beam's lane centre (local): the mouth plus half the beam along the fire direction.
        Vector2 BeamCentre => ArrowMath.Centre(mouth, ArrowMath.Direction(direction, angleDegrees), beamLength, 0f);

        Vector2 LocalPose(int s) => beam ? BeamCentre : angleDegrees == 0f
            ? new(ArrowMath.CentreX(mouth.x, direction, arrowLength, ArrowMath.Offset(s, tellTicks, unitsPerTick, travel)), mouth.y)
            : ArrowMath.Centre(mouth, ArrowMath.Direction(direction, angleDegrees), arrowLength, ArrowMath.Offset(s, tellTicks, unitsPerTick, travel));

        bool CatGravityUp()
        {
            ObserverContext observer = observers.Get(Reality.Id);
            return observer != null && observer.Gravity != null && observer.Gravity.Direction.y > 0f;
        }

        // PAX-107 (D-123): the launcher (and with it the arrow and its kill box) on the authored or the mirrored lane.
        void SetLane(bool up)
        {
            mirrored = up;
            transform.localPosition = home + (up ? new Vector3(0f, mirrorShift, 0f) : Vector3.zero);
        }

        void ShowUnfired()
        {
            if (arrow == null || launcher == null) return;
            arrow.enabled = false;
            if (ray != null) ray.enabled = false;
            arrow.transform.localPosition = LocalPose(0);
            launcher.color = unfiredColor;
            if (shaft != null) shaft.enabled = false;
        }

        // PAX-090 (D-091): the pose, the look and the shaft all follow from the timing (the live step's rules at roomTick).
        protected override void CaptureExtra(ref TrapSnapshot snapshot) => snapshot.ExtraInt = mirrored ? 1 : 0;

        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick)
        {
            if (mirrorWithGravity) SetLane(snapshot.ExtraInt == 1);
            if (!enabled || !IsTimingEffectActive) { ShowUnfired(); return; }
            int s = roomTick - LatestFireTick;
            ShowShot(s);
            if (shaft != null) shaft.enabled = spear && s >= ArrowMath.StuckCheckTick(tellTicks, flightTicks);
        }

        protected override void OnReset() { if (mirrorWithGravity) SetLane(false); ShowUnfired(); }
        protected override void OnTimingRearmed() => ShowUnfired();
    }
}
