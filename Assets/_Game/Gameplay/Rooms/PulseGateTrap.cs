using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>PAX-112 (D-176): a fixed laser barrier (its box) that turns on and off on a Periodic cycle. The fire tick is the
    /// warning's first tick: for warnTicks the barrier flickers dim and is harmless; then for onTicks it is drawn full and its
    /// Hazard is armed (a touch kills through RoomDeath, as hidden spikes do); then it is off until the next fire. The phase is
    /// a pure function of room ticks since the latest fire, so the death hold and the pause freeze it, the room reset restarts
    /// it, and a checkpoint rewind redraws it from the restored timing (D-091).</summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class PulseGateTrap : RoomTrap
    {
        [SerializeField] Hazard hazard;
        [SerializeField] int warnTicks = PulseGateMath.MinWarnTicks, onTicks = 20;
        [SerializeField] float warnAlpha = .45f, warnDimAlpha = .15f;

        SpriteRenderer visual;
        Color onColor;

        public PulseGatePhase Phase { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            visual = GetComponent<SpriteRenderer>();
            if (hazard == null) Debug.LogError($"PulseGateTrap on '{name}': missing Hazard", this);
            if (!IsPeriodic) Debug.LogError($"PulseGateTrap on '{name}': repeat mode must be Periodic (D-176); it never turns on otherwise.", this);
            if (hazard == null || visual == null) { enabled = false; return; }
            onColor = visual.color;
            Apply();
        }

        protected override CatDeathKind DeclaredDeathKind => CatDeathKind.Zapped;

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            StepTiming(false);
            Apply();
        }

        protected override void OnReset() => Apply();
        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick) => Apply();

        void Apply()
        {
            if (hazard == null || visual == null) return;
            int since = LatestFireTick < 0 ? -1 : RoomLifeTick - LatestFireTick;
            Phase = PulseGateMath.PhaseSince(since, warnTicks, onTicks);
            hazard.SetArmed(Phase == PulseGatePhase.On);
            visual.enabled = Phase != PulseGatePhase.Off;
            Color c = onColor;
            if (Phase == PulseGatePhase.Warn) c.a = onColor.a * (PulseGateMath.WarnLit(since) ? warnAlpha : warnDimAlpha);
            visual.color = c;
        }
    }
}
