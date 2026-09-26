using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-087 (D-089) §5: a ClimbVine in the PauseTestRig solo room (real ObserverSet, RoomManager, RoomDeath,
    /// LevelPause, CatInputRouter, LocalHumanDriver and CatMotor2D). EditMode doesn't simulate physics here, so the cat stays
    /// where it's put and each tick starts from zero velocity: the velocity after a tick is what that tick's motor step (and
    /// room step) set. The rig has no floor, so the cat is airborne. Its collider is x [-0.5, 0.5], y [-0.58, -0.02]; the
    /// vine is x [-0.3, 0.3] around VineX, y [-1, 3].</summary>
    public sealed class ClimbRuntimeTests : PauseTestBase
    {
        const float VineX = .2f;

        PauseTestRig rig;
        Scripted input;
        ClimbVine vine;

        sealed class Scripted : ICatCommandSource
        {
            public float Move, Climb;
            public bool Jump;
            public CatCommand Read() { var c = new CatCommand { Move = Move, Climb = Climb, JumpPressed = Jump, JumpHeld = Jump }; Jump = false; return c; }
            public void ResetTransientState() => Jump = false;
        }

        [SetUp]
        public void Build()
        {
            rig = PauseTestRig.Build(realInput: true);
            input = new Scripted();
            PauseTestRig.GetPrivate<List<ICatCommandSource>>(rig.Router, "validSources").Add(input);
        }

        [TearDown]
        public void Teardown() => rig?.Dispose();

        CatMotorConfig Config => PauseTestRig.GetPrivate<CatMotorConfig>(rig.Cat, "config");
        float Dt => TickTime.SecondsPerTick;
        float G => rig.CatGo.GetComponent<GravityReceiver>().Strength;
        float JumpSpeed => JumpMath.SpeedForHeight(Config.JumpHeight, G);

        // A vine in the cat's reality, then the driver re-activated so it finds it (as a scene load would).
        ClimbVine AddVine(bool snaps = false, int delay = 0)
        {
            Transform reality = rig.CatGo.transform.parent;
            var go = new GameObject("Vine");
            go.transform.SetParent(reality, false);
            go.transform.localPosition = new Vector2(VineX, 1f);
            go.layer = rig.CatGo.layer;
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(.6f, 4f);
            SpriteRenderer segment = new GameObject("Segment_0").AddComponent<SpriteRenderer>();
            segment.transform.SetParent(go.transform, false);

            ClimbVine v = go.AddComponent<ClimbVine>();
            PauseTestRig.SetPrivate(v, "roomId", 0);
            PauseTestRig.SetPrivate(v, "roomDeath", rig.Death);
            PauseTestRig.SetPrivate(v, "rooms", rig.Rooms);
            PauseTestRig.SetPrivate(v, "observers", rig.Observers);
            PauseTestRig.SetPrivate(v, "snaps", snaps);
            PauseTestRig.SetPrivate(v, "delayTicks", delay);
            PauseTestRig.SetPrivate(v, "segments", new[] { segment });
            PauseTestRig.Invoke(v, "Awake");
            PauseTestRig.Invoke(v, "OnEnable");
            Physics2D.SyncTransforms();
            rig.Observer.SetDriver(new LocalHumanDriver(rig.Router));
            return v;
        }

        void MoveCat(Vector2 local)
        {
            rig.CatGo.transform.position = rig.CatGo.transform.parent.TransformPoint(local);
            rig.CatBody.position = rig.CatGo.transform.position;
            Physics2D.SyncTransforms();
        }

        Vector2 Step()
        {
            rig.CatBody.linearVelocity = Vector2.zero;
            rig.Tick();
            Physics2D.SyncTransforms();
            return rig.CatBody.linearVelocity;
        }

        void Grab()
        {
            input.Climb = 1f;
            Step();
            Assert.IsTrue(rig.Cat.IsClimbing, "precondition: grabbed");
        }

        // ---------- grab ----------

        [Test]
        public void NoVine_Climb0_TheMotorsOwnStep_Unchanged()
        {
            vine = AddVine();
            Vector2 v = Step();
            Assert.IsFalse(rig.Cat.IsClimbing, "touching a vine without pushing does nothing");
            Assert.AreEqual(new Vector2(0f, -G * Dt), v, "the ordinary fall");
        }

        [Test]
        public void Airborne_PushUp_Grabs_SnapsXToTheVinesCentre_AndClimbsAtFourUnitsPerSecond()
        {
            vine = AddVine();
            input.Climb = 1f;
            Vector2 v = Step();
            Assert.IsTrue(rig.Cat.IsClimbing);
            Assert.AreSame(vine, rig.Cat.ClimbedVine);
            Assert.AreEqual(VineX, rig.CatBody.position.x, 1e-5f, "x snaps to the vine's centre on the grab tick");
            Assert.AreEqual(new Vector2(0f, Config.ClimbSpeed), v, "Climb × ClimbSpeed, no gravity");
            Assert.AreEqual(4f, Config.ClimbSpeed);
        }

        [Test]
        public void Airborne_PushDown_AlsoGrabs_AndClimbsDown()
        {
            vine = AddVine();
            input.Climb = -1f;
            Vector2 v = Step();
            Assert.IsTrue(rig.Cat.IsClimbing);
            Assert.AreEqual(new Vector2(0f, -4f), v);
        }

        [Test]
        public void BelowTheThreshold_NoGrab()
        {
            vine = AddVine();
            input.Climb = .49f;
            Step();
            Assert.IsFalse(rig.Cat.IsClimbing);
        }

        [Test]
        public void WhileClimbing_MoveAloneDoesNothing_AndClimb0Hangs()
        {
            vine = AddVine();
            Grab();
            input.Climb = 0f; input.Move = 1f;
            Vector2 v = Step();
            Assert.IsTrue(rig.Cat.IsClimbing);
            Assert.AreEqual(Vector2.zero, v);
        }

        [Test]
        public void AtTheTop_TheColliderTopStops_TheCatStaysOnTheVine()
        {
            vine = AddVine();
            Grab();
            MoveCat(new Vector2(VineX, 3f + .02f));   // collider top exactly at the vine's top (y 3)
            Assert.AreEqual(0f, Step().y, 1e-4f);
            Assert.IsTrue(rig.Cat.IsClimbing);
            MoveCat(new Vector2(VineX, 3f + .02f - .05f));
            Assert.AreEqual(.05f / Dt, Step().y, 1e-3f, "the last step lands on the top");
        }

        [Test]
        public void ClimbingDown_BelowTheBottom_Releases_AndTheCatFalls()
        {
            vine = AddVine();
            Grab();
            input.Climb = -1f;
            MoveCat(new Vector2(VineX, -1f + .3f - .01f));   // collider centre y -1.01, below the bottom (y -1)
            Vector2 v = Step();
            Assert.IsFalse(rig.Cat.IsClimbing);
            Assert.AreEqual(-G * Dt, v.y, 1e-4f, "the ordinary step, from rest");
        }

        // ---------- leap ----------

        [Test]
        public void Leap_WithMove_IsTheJumpPlusRunSpeed_NoCoyote_AndReleases()
        {
            vine = AddVine();
            Grab();
            input.Move = 1f; input.Jump = true;
            Vector2 v = Step();
            Assert.IsFalse(rig.Cat.IsClimbing);
            Assert.AreEqual(new Vector2(Config.MaxSpeed, JumpSpeed), v);
            Assert.AreEqual(0f, PauseTestRig.GetPrivate<float>(rig.Cat, "coyoteTimer"), "no coyote after a leap");
        }

        [Test]
        public void Leap_WithoutMove_IsStraightUp()
        {
            vine = AddVine();
            Grab();
            input.Jump = true;
            Assert.AreEqual(new Vector2(0f, JumpSpeed), Step());
        }

        [Test]
        public void ABufferedJump_OnTheGrabTick_Leaps()
        {
            vine = AddVine();
            MoveCat(new Vector2(5f, 0f));             // away from the vine
            input.Jump = true;
            Step();                                   // airborne, no coyote: the press is buffered
            MoveCat(Vector2.zero);
            input.Climb = 1f; input.Move = -1f;
            Vector2 v = Step();
            Assert.AreEqual(new Vector2(-Config.MaxSpeed, JumpSpeed), v, "grabbed and leapt on the same tick");
            Assert.IsFalse(rig.Cat.IsClimbing);
        }

        [Test]
        public void AfterALeap_TheSameVine_CantBeRegrabbedForTenTicks()
        {
            vine = AddVine();
            Grab();
            input.Jump = true;
            Step();
            Assert.AreEqual(10, Config.RegrabLockTicks);
            for (int i = 1; i <= 10; i++)
            {
                MoveCat(Vector2.zero);
                Step();
                Assert.IsFalse(rig.Cat.IsClimbing, $"tick {i} after the leap: locked");
            }
            MoveCat(Vector2.zero);
            Step();
            Assert.IsTrue(rig.Cat.IsClimbing, "the eleventh tick regrabs");
        }

        // ---------- release causes ----------

        [Test]
        public void AGravityFlip_Releases_AndTheCatFallsTheNewWay()
        {
            vine = AddVine();
            Grab();
            rig.CatGo.GetComponent<GravityReceiver>().Flip();
            Vector2 v = Step();
            Assert.IsFalse(rig.Cat.IsClimbing);
            Assert.AreEqual(G * Dt, v.y, 1e-4f, "the ordinary step under gravity up");
        }

        [Test]
        public void ALaunch_Releases_WithTheRegrabLock()
        {
            vine = AddVine();
            Grab();
            rig.Cat.ApplyLaunch(Vector2.up, 14f);
            Assert.IsFalse(rig.Cat.IsClimbing);
            Assert.AreEqual(14f, rig.CatBody.linearVelocity.y);
            for (int i = 0; i < 10; i++) { MoveCat(Vector2.zero); Step(); Assert.IsFalse(rig.Cat.IsClimbing, $"locked, tick {i + 1}"); }
            MoveCat(Vector2.zero);
            Step();
            Assert.IsTrue(rig.Cat.IsClimbing);
        }

        [Test]
        public void TheDeathHold_FreezesTheClimb_AndTheRespawnClearsIt()
        {
            vine = AddVine();
            Grab();
            rig.Death.Kill(ObserverId.A, DeathCause.Hazard);
            for (int i = 0; ; i++)
            {
                Assert.Less(i, 100, "the hold never ended");
                input.Climb = 1f;
                Vector2 v = Step();
                if (!rig.Death.IsHolding) break;
                Assert.IsTrue(rig.Cat.IsClimbing, "the hold shows the room exactly as it killed");
                Assert.AreEqual(Vector2.zero, v, "frozen");
            }
            Assert.IsFalse(rig.Cat.IsClimbing, "the respawn ends the climb");
        }

        [Test]
        public void ResetMotion_ClearsTheClimb_WithNoLock()
        {
            vine = AddVine();
            Grab();
            rig.Cat.ResetMotion();
            Assert.IsFalse(rig.Cat.IsClimbing);
            MoveCat(Vector2.zero);
            Step();
            Assert.IsTrue(rig.Cat.IsClimbing, "no lock after a reset");
        }

        [Test]
        public void ThePause_FreezesTheClimb()
        {
            vine = AddVine();
            Grab();
            Vector2 before = rig.CatBody.position;
            Assert.IsTrue(rig.Pause.Pause());
            input.Climb = -1f; input.Jump = true;
            for (int i = 0; i < 20; i++) rig.Tick();
            Assert.IsTrue(rig.Cat.IsClimbing, "paused: no motor step ran");
            Assert.AreEqual(before, rig.CatBody.position);
        }

        [Test]
        public void TheDriversDeactivate_EndsTheClimb()
        {
            vine = AddVine();
            Grab();
            rig.Observer.SetDriver(new PauseTestRig.CommandingDriver { Observer = rig.Observer });
            Assert.IsFalse(rig.Cat.IsClimbing);
        }

        // ---------- snap vines (§2.4) ----------

        [Test]
        public void ASnapVine_Fires_Vanishes_ReleasesTheCatInTheRoomStep_AndResetsWithTheRoom()
        {
            vine = AddVine(snaps: true, delay: 2);
            SpriteRenderer segment = PauseTestRig.GetPrivate<SpriteRenderer[]>(vine, "segments")[0];
            input.Climb = 1f;
            Step();
            Assert.IsTrue(rig.Cat.IsClimbing);
            int ticks = 0;
            while (!vine.IsSnapped) { Assert.Less(++ticks, 10, "never snapped"); MoveCat(new Vector2(VineX, 0f)); Step(); }
            Assert.IsFalse(rig.Cat.IsClimbing, "released in the snap's room step");
            Assert.IsFalse(segment.enabled, "vanished");
            Assert.IsFalse(vine.IsGrabbable);
            Vector2 v = Step();
            Assert.IsFalse(rig.Cat.IsClimbing, "a snapped vine can't be grabbed");
            Assert.AreEqual(-G * Dt, v.y, 1e-4f, "the cat falls from the next motor step");

            rig.Death.Kill(ObserverId.A, DeathCause.Hazard);
            for (int i = 0; i < 100 && rig.Death.IsHolding; i++) { input.Climb = 0f; Step(); }
            Assert.IsFalse(vine.IsSnapped, "the room reset restores it");
            Assert.IsTrue(segment.enabled);
            Assert.AreEqual(-1, vine.LatestFireTick);
        }

        [Test]
        public void AVineWithoutSnapSettings_NeverFires()
        {
            vine = AddVine();
            input.Climb = 1f;
            for (int i = 0; i < 30; i++) { MoveCat(new Vector2(VineX, 0f)); Step(); }
            Assert.IsFalse(vine.IsSnapped);
            Assert.AreEqual(-1, vine.LatestFireTick);
        }
    }
}
