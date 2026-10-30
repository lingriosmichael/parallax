using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>PAX-116 (D-187): a spinning hazard. Spiked arms turn about this object's position (the pivot) at degreesPerTick:
    /// from the room's first tick (alwaysOn), or from the trap's fire (Overlap or Chain). The angle is a function of room ticks
    /// (TrapPath.SpinAngle), so the death hold and the pause freeze it, a reset returns it to startAngle and a rewind restores
    /// it exactly (D-091). A cat overlapping any arm (an oriented box, Physics2D.OverlapBox) dies; the hub is harmless. The
    /// arms are drawn by the "_Arm" children of the rotor child, which turns (the trap and its trigger never do).</summary>
    public sealed class SpinnerTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] BoxCollider2D trigger;
        [SerializeField] Transform rotor;
        [SerializeField] int arms = 1;
        [SerializeField] float armLength = 2f, armWidth = .3f;
        [Tooltip("Degrees per tick; positive turns counter-clockwise.")]
        [SerializeField] float degreesPerTick = 3f;
        [SerializeField] float startAngle;
        [Tooltip("Turns from the room's first tick; otherwise from the trap's fire.")]
        [SerializeField] bool alwaysOn = true;
        [SerializeField] int delayTicks;
        [Header("PAX-118 (D-197) the pendulum (off at swingPeriod 0)")]
        [Tooltip("Swings startAngle ± swingAmplitude degrees, once every swingPeriod ticks, instead of turning.")]
        [SerializeField] float swingAmplitude;
        [SerializeField] int swingPeriod;

        ContactFilter2D filter;
        readonly Collider2D[] results = new Collider2D[8];

        protected override int DelayTicks => delayTicks;
        protected override CatDeathKind DeclaredDeathKind => CatDeathKind.Spiked;
        public float Angle { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (observers == null || Reality == null || rotor == null || (!alwaysOn && UsesOverlapSource && trigger == null))
            { Debug.LogError($"SpinnerTrap '{name}': missing observers, reality, rotor or (a triggered one) its trigger.", this); enabled = false; return; }
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
            Pose(startAngle);
        }

        /// <summary>The angle `roomTick`: turning since tick 0 (alwaysOn) or since the fire; startAngle before.</summary>
        float AngleAt(int roomTick)
        {
            int ticks = alwaysOn ? roomTick : IsTimingEffectActive || LatestFireTick >= 0 ? roomTick - LatestFireTick : -1;
            if (ticks < 0) return startAngle;
            return swingPeriod > 0 ? TrapPath.SwingAngle(startAngle, swingAmplitude, swingPeriod, ticks) : TrapPath.SpinAngle(startAngle, degreesPerTick, ticks);
        }

        void Pose(float angle) { Angle = angle; if (rotor != null) rotor.localRotation = Quaternion.Euler(0f, 0f, angle); }

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            if (!alwaysOn) StepTiming(trigger != null && IsLocalHumanOverlapping(trigger, observers, filter, results, out _));
            Pose(AngleAt(RoomLifeTick));
            if (ArmHits()) Death.Kill(Reality.Id, DeathCause.Hazard, this);
        }

        /// <summary>A cat overlaps an arm at the current angle (the kill test, also the route harness's).</summary>
        public bool ArmHits()
        {
            if (!enabled || observers == null) return false;
            Vector2 pivot = transform.position;
            for (int i = 0; i < arms; i++)
            {
                Vector2 centre = TrapPath.ArmCentre(pivot, Angle, i, arms, armLength);
                if (IsLocalHumanOverlapping(new Bounds(centre, new Vector3(armLength, armWidth)), TrapPath.ArmAngle(Angle, i, arms), observers, filter, results, out _)) return true;
            }
            return false;
        }

        protected override void OnReset() => Pose(startAngle);
        protected override void OnTimingRearmed() { }
        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick) => Pose(AngleAt(roomTick));
    }
}
