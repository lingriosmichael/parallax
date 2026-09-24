using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-074 (D-078) §5.3/§5.4 and R8: ArrowTrap in the PauseTestRig solo room (real
    /// ObserverSet, RoomManager, RoomDeath). EditMode doesn't simulate physics, so the cat stays where
    /// it's put. Its collider is a 1 x 0.56 box centred 0.3 below its root, at (0, -0.3).</summary>
    public sealed class ArrowTrapRuntimeTests
    {
        static readonly Color Disguise = new(.72f, .52f, .28f, 1f), Honest = new(.20f, .20f, .23f, 1f);

        static ArrowTrap AddArrow(PauseTestRig rig, string name, Vector2 launcherCentre, ArrowDirection direction, float laneLength, float laneY,
            TrapTriggerSource source = TrapTriggerSource.Overlap, TrapRepeatMode repeat = TrapRepeatMode.Once, int cooldown = 0, int period = 1, int phase = 0,
            int delay = 0, RoomTrap chainSource = null, Vector2? triggerCentre = null, Vector2 triggerSize = default)
        {
            Transform reality = rig.CatGo.transform.parent;
            var go = new GameObject(name);
            go.transform.SetParent(reality, false);
            go.transform.localPosition = launcherCentre;
            SpriteRenderer launcher = go.AddComponent<SpriteRenderer>();
            launcher.color = Disguise;
            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(go.transform, false);
            SpriteRenderer arrow = arrowGo.AddComponent<SpriteRenderer>();

            BoxCollider2D trigger = null;
            if (triggerCentre.HasValue)
            {
                var triggerGo = new GameObject("Trigger");
                triggerGo.transform.SetParent(go.transform, false);
                triggerGo.transform.position = reality.TransformPoint(triggerCentre.Value);
                trigger = triggerGo.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = triggerSize;
            }

            float sign = direction == ArrowDirection.Right ? 1f : -1f;
            ArrowTrap trap = go.AddComponent<ArrowTrap>();
            PauseTestRig.SetPrivate(trap, "roomId", 0);
            PauseTestRig.SetPrivate(trap, "roomDeath", rig.Death);
            PauseTestRig.SetPrivate(trap, "rooms", rig.Rooms);
            PauseTestRig.SetPrivate(trap, "triggerSource", source);
            PauseTestRig.SetPrivate(trap, "repeatMode", repeat);
            PauseTestRig.SetPrivate(trap, "cooldownTicks", cooldown);
            PauseTestRig.SetPrivate(trap, "periodTicks", period);
            PauseTestRig.SetPrivate(trap, "phaseTicks", phase);
            PauseTestRig.SetPrivate(trap, "chainSource", chainSource);
            PauseTestRig.SetPrivate(trap, "observers", rig.Observers);
            PauseTestRig.SetPrivate(trap, "trigger", trigger);
            PauseTestRig.SetPrivate(trap, "launcher", launcher);
            PauseTestRig.SetPrivate(trap, "arrow", arrow);
            PauseTestRig.SetPrivate(trap, "direction", direction);
            PauseTestRig.SetPrivate(trap, "mouth", new Vector2(sign * .25f, laneY - launcherCentre.y));
            PauseTestRig.SetPrivate(trap, "travel", laneLength - .8f);
            PauseTestRig.SetPrivate(trap, "delayTicks", delay);
            PauseTestRig.SetPrivate(trap, "honestColor", Honest);
            PauseTestRig.Invoke(trap, "Awake");
            PauseTestRig.Invoke(trap, "OnEnable");
            Physics2D.SyncTransforms();
            return trap;
        }

        static SpriteRenderer ArrowOf(ArrowTrap trap) => PauseTestRig.GetPrivate<SpriteRenderer>(trap, "arrow");
        static SpriteRenderer LauncherOf(ArrowTrap trap) => PauseTestRig.GetPrivate<SpriteRenderer>(trap, "launcher");

        // ---------- §5.3 kill and survive ----------

        // Periodic, phase 0: fires on the first room tick. Mouth x -5, lane y -0.3 (the cat's centre
        // line). The arrow's front is at -4.2 + 0.3 n on flight tick n; the cat's box starts at -0.5, so
        // n 12 (front -0.6) misses and n 13 (front -0.3) hits: s = 6 + 13 = 19, the 20th room tick.
        [Test]
        public void StandingCatInTheLane_IsKilledThroughRoomDeath_OnThePredictedTick_AndTheHoldFreezesTheArrow()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: false, holdTicks: 30);
            try
            {
                DeathCause? cause = null;
                rig.Death.Died += info => cause = info.Cause;
                ArrowTrap trap = AddArrow(rig, "ArrowA", new Vector2(-5.25f, -.3f), ArrowDirection.Right, 10f, -.3f, repeat: TrapRepeatMode.Periodic, cooldown: 200, period: 400);

                for (int i = 0; i < 19; i++) rig.Tick();
                Assert.AreEqual(0, rig.Death.DeathsIn(0), "no kill before s = 19");
                rig.Tick();
                Assert.AreEqual(1, rig.Death.DeathsIn(0), "killed on s = 19");
                Assert.IsTrue(rig.Death.IsHolding, "the kill starts the death hold");

                Vector3 frozen = ArrowOf(trap).transform.position;
                for (int i = 0; i < 10; i++) rig.Tick();
                Assert.AreEqual(frozen, ArrowOf(trap).transform.position, "the hold freezes the arrow");
                for (int i = 0; i < 25; i++) rig.Tick();
                Assert.AreEqual(DeathCause.Hazard, cause, "the death goes through RoomDeath as a hazard");
            }
            finally { rig.Dispose(); }
        }

        // The lane at y -0.71 (band -0.79 to -0.63) runs 0.05 under the cat's box (bottom -0.58). The
        // kill test is Physics2D.OverlapBox, like every trap's, so a band within Box2D's contact skin
        // (~0.01 u) of the cat still counts as touching: "fully above" needs more than the skin.
        [Test]
        public void CatFullyAboveTheLane_IsMissed_AndTheStoppedArrowStaysVisibleAtTheLaneEnd()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: false, holdTicks: 30);
            try
            {
                ArrowTrap trap = AddArrow(rig, "ArrowA", new Vector2(-5.25f, -.71f), ArrowDirection.Right, 10f, -.71f, repeat: TrapRepeatMode.Periodic, cooldown: 200, period: 400);
                for (int i = 0; i < 60; i++) rig.Tick();
                Assert.AreEqual(0, rig.Death.DeathsIn(0), "an arrow under the cat must not kill it");
                Assert.IsTrue(ArrowOf(trap).enabled, "a stopped arrow stays visible");
                Assert.AreEqual(5f - .4f, ArrowOf(trap).transform.position.x, 1e-4f, "stopped with its front face at the lane end x 5");
            }
            finally { rig.Dispose(); }
        }

        // ---------- §5.4 reset, rearm, disguise ----------

        // Lane at y 3, far above the cat, so the arrow never kills; the kill comes from the test.
        [Test]
        public void RoomReset_RestoresTheAuthoredState([Values(TrapRepeatMode.Once, TrapRepeatMode.Rearm, TrapRepeatMode.Periodic)] TrapRepeatMode repeat)
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: false, holdTicks: 0);
            try
            {
                bool periodic = repeat == TrapRepeatMode.Periodic;
                ArrowTrap trap = AddArrow(rig, "Arrow", new Vector2(-5.25f, 3f), ArrowDirection.Right, 4f, 3f, repeat: repeat, cooldown: periodic ? 60 : 40, period: periodic ? 120 : 1,
                    triggerCentre: periodic ? (Vector2?)null : Vector2.zero, triggerSize: new Vector2(2f, 2f));
                Vector3 start = ArrowOf(trap).transform.localPosition;
                Assert.IsFalse(ArrowOf(trap).enabled, "unfired: the arrow is hidden");
                Assert.AreEqual(Disguise, LauncherOf(trap).color, "unfired: the launcher keeps its authored look");

                rig.Tick();
                Assert.AreEqual(0, trap.LatestFireTick, "fired on the first tick");
                Assert.IsTrue(ArrowOf(trap).enabled, "the tell shows the arrow on the fire tick");
                Assert.AreEqual(Honest, LauncherOf(trap).color, "the launcher shows the honest look on fire");
                for (int i = 0; i < 12; i++) rig.Tick();
                Assert.AreNotEqual(start, ArrowOf(trap).transform.localPosition, "the arrow is in flight");

                rig.Death.Kill(ObserverId.A, DeathCause.Hazard);

                Assert.AreEqual(TrapState.Armed, trap.State);
                Assert.AreEqual(-1, trap.LatestFireTick, "un-fired");
                Assert.IsFalse(ArrowOf(trap).enabled, "unseen");
                Assert.AreEqual(start, ArrowOf(trap).transform.localPosition, "back at the launcher");
                Assert.AreEqual(Disguise, LauncherOf(trap).color, "disguise restored");
            }
            finally { rig.Dispose(); }
        }

        // Travel 2.2, N 8: stopped from s 15. Cooldown 40: the arrow snaps back on room tick 40.
        [Test]
        public void Rearm_SnapsBackAtTheEndOfItsCooldown()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: false, holdTicks: 30);
            try
            {
                ArrowTrap trap = AddArrow(rig, "Arrow", new Vector2(-5.25f, 3f), ArrowDirection.Right, 3f, 3f, repeat: TrapRepeatMode.Rearm, cooldown: 40,
                    triggerCentre: Vector2.zero, triggerSize: new Vector2(2f, 2f));
                Vector3 start = ArrowOf(trap).transform.localPosition;
                rig.Tick();
                Assert.AreEqual(0, trap.LatestFireTick);
                rig.CatGo.transform.position = new Vector2(20f, 0f);
                Physics2D.SyncTransforms();

                for (int i = 1; i < 40; i++) rig.Tick();
                Assert.IsTrue(ArrowOf(trap).enabled, "stopped and still visible on room tick 39");
                Assert.AreEqual(TrapState.Fired, trap.State);
                rig.Tick();
                Assert.AreEqual(TrapState.Armed, trap.State, "rearmed on room tick 40");
                Assert.IsFalse(ArrowOf(trap).enabled, "hidden again");
                Assert.AreEqual(start, ArrowOf(trap).transform.localPosition, "snapped back to the launcher");
            }
            finally { rig.Dispose(); }
        }

        // ---------- R8: a chained trap fires delay ticks after the arrow's s = 0 ----------

        [Test]
        public void ChainedTrap_FiresExactlyDelayTicksAfterTheArrowsFire()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: false, holdTicks: 30);
            try
            {
                ArrowTrap source = AddArrow(rig, "Source", new Vector2(-5.25f, 3f), ArrowDirection.Right, 4f, 3f, triggerCentre: new Vector2(0f, -.3f), triggerSize: new Vector2(2f, 2f));
                ArrowTrap target = AddArrow(rig, "Target", new Vector2(-5.25f, 5f), ArrowDirection.Right, 4f, 5f, source: TrapTriggerSource.Chain, delay: 5, chainSource: source);
                for (int i = 0; i < 12; i++) rig.Tick();
                Assert.AreEqual(0, source.LatestFireTick, "the arrow fires (s = 0) on the first tick");
                Assert.AreEqual(5, target.LatestFireTick, "the chained trap fires 5 ticks after the arrow's s = 0");
            }
            finally { rig.Dispose(); }
        }
    }
}
