using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-088 (D-090) §5, §2.2-2.6: a StormCloudTrap in the PauseTestRig solo room (real ObserverSet, RoomManager,
    /// RoomDeath, LevelPause and CatMotor2D). EditMode doesn't simulate physics here, so the cat's body stays where a test
    /// puts it. The rig's cat: body (0, 0), box collider 1 x 0.56 at offset (0, -0.3), so its box is x [-0.5, 0.5],
    /// y [-0.58, -0.02]. The cloud is 2 x 0.8 at local (x, 3): its bottom is y 2.6. Its baked profile is one floor whose top
    /// is the cat's feet (y -0.58), plus an overhang in the overhang tests. The wake trigger is a 1 x 1 box on the cat.</summary>
    public sealed class StormCloudRuntimeTests : PauseTestBase
    {
        const int Delay = 50, Period = 100, Tell = 25, Strike = 6;
        const float CloudY = 3f, FeetY = -.58f;

        PauseTestRig rig;
        StormCloudTrap trap;

        [SetUp] public void Build() => rig = PauseTestRig.Build(realInput: true);
        [TearDown] public void Teardown() => rig?.Dispose();

        StormCloudTrap AddCloud(float x = 0f, float triggerX = 0f, float rangeMin = -20f, float rangeMax = 20f, Vector3[] extraTops = null, float cloudY = CloudY)
        {
            Transform reality = rig.CatGo.transform.parent;
            var go = new GameObject("StormCloud");
            go.transform.SetParent(reality, false);
            go.transform.localPosition = new Vector2(x, cloudY);
            go.layer = rig.CatGo.layer;
            SpriteRenderer Child(string name)
            {
                var c = new GameObject(name);
                c.transform.SetParent(go.transform, false);
                return c.AddComponent<SpriteRenderer>();
            }
            SpriteRenderer cloud = Child("Cloud"), target = Child("Target"), bolt = Child("Bolt");
            var triggerGo = new GameObject("Trigger");
            triggerGo.transform.SetParent(go.transform, false);
            triggerGo.transform.position = reality.TransformPoint(new Vector2(triggerX, -.3f));
            triggerGo.layer = rig.CatGo.layer;
            BoxCollider2D trigger = triggerGo.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = Vector2.one;

            // Profile (xMin, xMax, top), relative to the cloud's authored pose.
            var profile = new System.Collections.Generic.List<Vector3> { new(-30f - x, 30f - x, FeetY - cloudY) };
            if (extraTops != null) foreach (Vector3 t in extraTops) profile.Add(new Vector3(t.x - x, t.y - x, t.z - cloudY));

            StormCloudTrap t2 = go.AddComponent<StormCloudTrap>();
            PauseTestRig.SetPrivate(t2, "roomId", 0);
            PauseTestRig.SetPrivate(t2, "roomDeath", rig.Death);
            PauseTestRig.SetPrivate(t2, "rooms", rig.Rooms);
            PauseTestRig.SetPrivate(t2, "observers", rig.Observers);
            PauseTestRig.SetPrivate(t2, "triggerSource", TrapTriggerSource.Overlap);
            PauseTestRig.SetPrivate(t2, "repeatMode", TrapRepeatMode.Once);
            PauseTestRig.SetPrivate(t2, "trigger", trigger);
            PauseTestRig.SetPrivate(t2, "cloud", cloud);
            PauseTestRig.SetPrivate(t2, "target", target);
            PauseTestRig.SetPrivate(t2, "bolt", bolt);
            PauseTestRig.SetPrivate(t2, "rangeMin", rangeMin - x);
            PauseTestRig.SetPrivate(t2, "rangeMax", rangeMax - x);
            PauseTestRig.SetPrivate(t2, "profile", profile.ToArray());
            PauseTestRig.Invoke(t2, "Awake");
            PauseTestRig.Invoke(t2, "OnEnable");
            Physics2D.SyncTransforms();
            return t2;
        }

        void MoveCat(float x)
        {
            rig.CatGo.transform.position = rig.CatGo.transform.parent.TransformPoint(new Vector2(x, 0f));
            Physics2D.SyncTransforms();
        }

        float CloudX => trap.transform.localPosition.x + trap.OffsetX;
        int SinceWake => trap.LatestFireTick < 0 ? -1 : rig.Rooms.RoomLifeTick - trap.LatestFireTick;

        void Step() { rig.CatBody.linearVelocity = Vector2.zero; rig.Tick(); }

        // Bounded: a phase that never comes fails instead of hanging the Editor.
        void StepUntil(StormCloudPhase phase)
        {
            for (int i = 0; i < 3 * Period; i++) { if (trap.Phase == phase) return; Step(); }
            Assert.Fail($"the cloud never reached {phase}");
        }

        [Test]
        public void Dormant_UntilItsTriggerFires_ItNeverMoves_AndNeverStrikes()
        {
            trap = AddCloud(x: -3f, triggerX: 10f);
            for (int i = 0; i < 2 * Period; i++)
            {
                Step();
                Assert.AreEqual(StormCloudPhase.Dormant, trap.Phase, $"room tick {rig.Rooms.RoomLifeTick}");
                Assert.AreEqual(-3f, CloudX, 1e-6f);
                Assert.IsFalse(rig.Death.IsHolding);
            }
            Assert.AreEqual(-1, trap.LatestFireTick);
        }

        [Test]
        public void Awake_ItFollowsTheCatsBodyX_AtTheFollowSpeed_FromWakePlusOne_AndLandsOnIt()
        {
            trap = AddCloud(x: -3f);
            Step();
            Assert.AreEqual(0, SinceWake, "woken on the first room tick (the cat is on its trigger)");
            Assert.AreEqual(-3f, CloudX, 1e-6f, "the wake tick doesn't move it");
            for (int s = 1; s < Delay; s++)
            {
                Step();
                Assert.AreEqual(StormCloudPhase.Follow, trap.Phase, $"s {SinceWake}");
                Assert.AreEqual(Mathf.Min(0f, -3f + .08f * s), CloudX, 1e-4f, $"s {s}");
            }
        }

        [Test]
        public void Follow_IsClampedToItsRange()
        {
            trap = AddCloud(x: -3f, rangeMin: -5f, rangeMax: -2f);
            for (int i = 0; i < Delay; i++) Step();
            Assert.AreEqual(-2f, CloudX, 1e-6f);
        }

        [Test]
        public void TheCharge_LocksItsX_AndTheStrikeUsesTheLockedX()
        {
            trap = AddCloud(x: 0f);
            StepUntil(StormCloudPhase.Charge);
            float locked = CloudX;
            MoveCat(5f);   // the cat walks off; the cloud mustn't follow while charging or striking
            for (int i = 1; i < Tell + Strike; i++)   // s 51..80: the rest of the charge and the strike
            {
                Step();
                Assert.AreEqual(locked, CloudX, 1e-6f, $"s {SinceWake} {trap.Phase}");
            }
            Assert.AreEqual(StormCloudPhase.Strike, trap.Phase);
            Assert.IsFalse(rig.Death.IsHolding, "the cat left the column");
            Step();
            Assert.AreEqual(StormCloudPhase.Follow, trap.Phase);
            Assert.Greater(CloudX, locked, "following again after the strike");
        }

        [Test]
        public void ACatStandingStillUnderIt_DiesOnTheFirstStrikeTick_ByHazard_AndNotBefore()
        {
            trap = AddCloud(x: 0f);
            DeathCause? cause = null;
            rig.Death.Died += d => cause = d.Cause;
            int killedAt = -1;
            for (int i = 0; i < Delay + Tell + Strike + 5 && killedAt < 0; i++)
            {
                Step();
                if (rig.Death.IsHolding) killedAt = SinceWake;
            }
            Assert.AreEqual(Delay + Tell, killedAt, "killed on wake + 75");
            for (int i = 0; i < 40 && cause == null; i++) Step();
            Assert.AreEqual(DeathCause.Hazard, cause);
        }

        [Test]
        public void ACatJustBesideTheColumn_Survives()
        {
            trap = AddCloud(x: 0f, rangeMin: 0f, rangeMax: 0f);   // pinned at x 0: column x [-0.4, 0.4]
            // The trigger sits at x 0: wake it with the cat on it, then move off to box x [0.5, 1.5].
            Step(); MoveCat(1f);
            for (int i = 0; i < Delay + Period; i++) { Step(); Assert.IsFalse(rig.Death.IsHolding, $"s {SinceWake}"); }
        }

        [Test]
        public void AnOverhangOverTheCat_StopsTheStrike_OnItsTop_AndTheCatSurvives()
        {
            // Overhang x [-1.5, 1.5], top y 0.5 (over the cat's head, below the cloud).
            trap = AddCloud(x: 0f, extraTops: new[] { new Vector3(-1.5f, 1.5f, .5f) });
            StepUntil(StormCloudPhase.Strike);
            Assert.AreEqual(.5f, trap.StrikeColumn.yMin - rig.CatGo.transform.parent.position.y, 1e-4f);
            Assert.AreEqual(CloudY - .4f, trap.StrikeColumn.yMax - rig.CatGo.transform.parent.position.y, 1e-4f);
            for (int i = 0; i < Period; i++) { Step(); Assert.IsFalse(rig.Death.IsHolding); }
        }

        // Ruling C on the path that kills: the cloud's bottom at y 0, the cat's body at (0, 0). Gravity down (body rotation 0):
        // the collider offset (0, -0.3) puts its box at y [-0.58, -0.02], inside the column: struck. Gravity up (the motor
        // turns the body 180): the offset turns with it, the box is y [0.02, 0.58], wholly above the cloud's bottom: safe.
        int StrikeKills(bool gravityUp)
        {
            if (gravityUp) rig.CatGo.GetComponent<GravityReceiver>().SetTargetDirection(Vector2.up, true);
            trap = AddCloud(x: 0f, cloudY: .4f);
            for (int i = 0; i < Delay + Tell + Strike + 5; i++)
            {
                Step();   // no physics here: the body stays at (0, 0); the motor sets its rotation each step
                if (rig.Death.IsHolding) return SinceWake;
            }
            return -1;
        }

        [Test] public void AGravityDownCat_UnderTheCloudsBottom_IsStruck() => Assert.AreEqual(Delay + Tell, StrikeKills(false));

        [Test]
        public void AGravityUpCat_AtTheSameBodyPosition_IsAboveTheCloudsBottom_AndSurvives()
        {
            Assert.AreEqual(-1, StrikeKills(true), "struck: the collider offset wasn't turned with the body");
            Assert.AreEqual(180f, Mathf.Abs(rig.CatBody.rotation), 1e-3f, "the motor turned the body");
        }

        [Test]
        public void Disabled_ItShowsItsAuthoredPose_Dormant()
        {
            trap = AddCloud(x: -3f);
            for (int i = 0; i < 20; i++) Step();
            Assert.AreNotEqual(0f, trap.OffsetX);
            trap.enabled = false;
            PauseTestRig.Invoke(trap, "OnDisable");
            Assert.AreEqual(0f, trap.OffsetX);
            Assert.AreEqual(0f, trap.Cloud.transform.localPosition.x);
            Assert.AreEqual(StormCloudPhase.Dormant, trap.Phase);
        }

        [Test]
        public void TheLook_ChargeDarkensAndDrawsTheTarget_StrikeDrawsTheBolt_FollowDrawsNeither()
        {
            trap = AddCloud(x: 0f);
            Step();
            Color idle = trap.Cloud.color;
            Assert.IsFalse(trap.Target.enabled); Assert.IsFalse(trap.Bolt.enabled);
            StepUntil(StormCloudPhase.Charge);
            Assert.AreNotEqual(idle, trap.Cloud.color, "the charge darkens the cloud");
            Assert.IsTrue(trap.Target.enabled, "the charge draws the target line");
            Assert.IsFalse(trap.Bolt.enabled);
            MoveCat(5f);
            StepUntil(StormCloudPhase.Strike);
            Assert.IsTrue(trap.Bolt.enabled); Assert.IsFalse(trap.Target.enabled);
            StepUntil(StormCloudPhase.Follow);
            Assert.IsFalse(trap.Bolt.enabled); Assert.IsFalse(trap.Target.enabled);
            Assert.AreEqual(idle, trap.Cloud.color);
        }

        [Test]
        public void TheDeathHold_FreezesIt_AndTheResetReturnsItToItsAuthoredPose_Dormant()
        {
            trap = AddCloud(x: -3f);
            for (int i = 0; i < 20; i++) Step();
            float x = CloudX;
            Assert.AreNotEqual(-3f, x);
            rig.Death.Kill(ObserverId.A, DeathCause.Hazard);
            for (int i = 0; ; i++)
            {
                Assert.Less(i, 100, "the hold never ended");
                MoveCat(8f);
                Step();
                if (!rig.Death.IsHolding) break;
                Assert.AreEqual(x, CloudX, 1e-6f, "frozen while holding");
                Assert.AreEqual(StormCloudPhase.Follow, trap.Phase);
            }
            Assert.AreEqual(StormCloudPhase.Dormant, trap.Phase);
            Assert.AreEqual(-3f, CloudX, 1e-6f, "back at its authored pose");
            Assert.AreEqual(-1, trap.LatestFireTick);
            Assert.IsFalse(trap.Target.enabled || trap.Bolt.enabled);
        }

        [Test]
        public void ThePause_FreezesIt()
        {
            trap = AddCloud(x: -3f);
            for (int i = 0; i < 10; i++) Step();
            float x = CloudX; int since = SinceWake;
            Assert.IsTrue(rig.Pause.Pause());
            for (int i = 0; i < 40; i++) rig.Tick();
            Assert.AreEqual(x, CloudX, 1e-6f);
            Assert.AreEqual(since, SinceWake);
            rig.Pause.Resume();
            rig.Pause.ApplyPendingResume();
            Step();
            Assert.AreEqual(since + 1, SinceWake);
            Assert.AreEqual(x + .08f, CloudX, 1e-4f);
        }
    }
}
