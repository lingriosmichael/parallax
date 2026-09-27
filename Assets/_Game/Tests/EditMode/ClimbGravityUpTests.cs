using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-090 R1 (D-092): a cat standing on the ceiling (gravity up) grabs a vine by pushing away from its ground,
    /// screen-down, and lets go by pushing into it, screen-up. The PauseTestRig solo room as in ClimbRuntimeTests: no physics
    /// runs, so the cat stays where it's put, touching the ceiling, and each tick starts from zero velocity. Upside down,
    /// the cat's collider is y [0.02, 0.58]; the ceiling's underside is at 0.6 (inside the 0.05 ground probe).</summary>
    public sealed class ClimbGravityUpTests : PauseTestBase
    {
        const float VineX = .2f;

        PauseTestRig rig;
        Scripted input;

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

            Transform reality = rig.CatGo.transform.parent;
            var ceiling = new GameObject("Ceiling") { layer = rig.CatGo.layer };
            ceiling.transform.SetParent(reality, false);
            ceiling.transform.localPosition = new Vector2(0f, .85f);
            ceiling.AddComponent<BoxCollider2D>().size = new Vector2(6f, .5f);

            var vineGo = new GameObject("Vine") { layer = rig.CatGo.layer };
            vineGo.transform.SetParent(reality, false);
            vineGo.transform.localPosition = new Vector2(VineX, -1.4f);
            BoxCollider2D box = vineGo.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(.6f, 4f);   // y [-3.4, 0.6]: its top meets the ceiling
            ClimbVine vine = vineGo.AddComponent<ClimbVine>();
            PauseTestRig.SetPrivate(vine, "roomId", 0);
            PauseTestRig.SetPrivate(vine, "roomDeath", rig.Death);
            PauseTestRig.SetPrivate(vine, "rooms", rig.Rooms);
            PauseTestRig.SetPrivate(vine, "observers", rig.Observers);
            PauseTestRig.Invoke(vine, "Awake");
            PauseTestRig.Invoke(vine, "OnEnable");

            rig.CatGo.GetComponent<GravityReceiver>().SetTargetDirection(Vector2.up, snap: true);
            rig.CatBody.rotation = 180f;
            Physics2D.SyncTransforms();
            rig.Observer.SetDriver(new LocalHumanDriver(rig.Router));
        }

        [TearDown]
        public void Teardown() => rig?.Dispose();

        CatMotorConfig Config => PauseTestRig.GetPrivate<CatMotorConfig>(rig.Cat, "config");
        float JumpSpeed => JumpMath.SpeedForHeight(Config.JumpHeight, rig.CatGo.GetComponent<GravityReceiver>().Strength);

        Vector2 Step()
        {
            rig.CatBody.linearVelocity = Vector2.zero;
            rig.CatBody.position = new Vector2(rig.CatBody.position.x, 0f);   // no physics: keep touching the ceiling
            Physics2D.SyncTransforms();
            rig.Tick();
            return rig.CatBody.linearVelocity;
        }

        // ---------- the pure rules ----------

        [Test]
        public void WantsGrab_Grounded_IsAwayFromTheGround_AirborneUnchanged()
        {
            const float t = ClimbState.DefaultGrabThreshold;
            Assert.IsTrue(ClimbState.WantsGrab(-.5f, true, t, gravityUp: true));
            Assert.IsFalse(ClimbState.WantsGrab(-.49f, true, t, gravityUp: true));
            Assert.IsFalse(ClimbState.WantsGrab(1f, true, t, gravityUp: true), "up is into the ceiling");
            Assert.IsTrue(ClimbState.WantsGrab(1f, true, t, gravityUp: false), "gravity down: PAX-087 unchanged");
            Assert.IsFalse(ClimbState.WantsGrab(-1f, true, t, gravityUp: false));
            Assert.IsTrue(ClimbState.WantsGrab(1f, false, t, gravityUp: true), "airborne: either direction");
            Assert.IsTrue(ClimbState.WantsGrab(-1f, false, t, gravityUp: true));
        }

        [Test]
        public void ReleasesAtBottom_Grounded_IsPushingIntoTheGround_TheBottomStopStaysScreenRelative()
        {
            Assert.IsTrue(ClimbState.ReleasesAtBottom(1f, 2f, 1f, true, gravityUp: true), "standing on the ceiling, pushing up");
            Assert.IsFalse(ClimbState.ReleasesAtBottom(-1f, 2f, 1f, true, gravityUp: true), "the grab tick: pushing away");
            Assert.IsTrue(ClimbState.ReleasesAtBottom(-1f, .99f, 1f, false, gravityUp: true), "below the vine's bottom, screen-down");
            Assert.IsFalse(ClimbState.ReleasesAtBottom(1f, .5f, 1f, false, gravityUp: true));
            Assert.IsTrue(ClimbState.ReleasesAtBottom(-1f, 2f, 1f, true, gravityUp: false), "gravity down: PAX-087 unchanged");
            Assert.IsFalse(ClimbState.ReleasesAtBottom(1f, 2f, 1f, true, gravityUp: false));
        }

        // ---------- the motor ----------

        [Test]
        public void Precondition_TheCatStandsOnTheCeiling()
        {
            Step();
            Assert.IsTrue(rig.Cat.IsGrounded, "grounded on the ceiling");
            Assert.IsFalse(rig.Cat.IsClimbing, "touching a vine without pushing does nothing");
        }

        [Test]
        public void OnTheCeiling_PushDown_Grabs_AndStillHoldsNextTick()
        {
            input.Climb = -1f;
            Vector2 v = Step();
            Assert.IsTrue(rig.Cat.IsClimbing, "pushing away from the ceiling grabs");
            Assert.AreEqual(VineX, rig.CatBody.position.x, 1e-5f, "x snaps to the vine's centre");
            Assert.AreEqual(new Vector2(0f, -Config.ClimbSpeed), v, "climbs screen-down");
            v = Step();
            Assert.IsTrue(rig.Cat.IsClimbing, "the grab tick's ground contact doesn't release it");
            Assert.AreEqual(new Vector2(0f, -Config.ClimbSpeed), v);
        }

        [Test]
        public void OnTheCeiling_PushUp_DoesNotGrab()
        {
            input.Climb = 1f;
            Step();
            Assert.IsFalse(rig.Cat.IsClimbing, "pushing into the ceiling isn't a grab");
            Step();
            Assert.IsFalse(rig.Cat.IsClimbing);
        }

        [Test]
        public void OnTheCeiling_ClimbsDown_ThenLeapsOff()
        {
            input.Climb = -1f;
            Step();
            Step();
            Assert.IsTrue(rig.Cat.IsClimbing, "precondition: climbing down");
            input.Jump = true;
            Vector2 v = Step();
            Assert.IsFalse(rig.Cat.IsClimbing, "the leap lets go");
            Assert.AreEqual(new Vector2(0f, -JumpSpeed), v, "the jump launch, away from the ceiling");
        }

        [Test]
        public void OnTheCeiling_PushingBackIntoTheCeiling_Releases()
        {
            input.Climb = -1f;
            Step();
            Assert.IsTrue(rig.Cat.IsClimbing, "precondition: grabbed");
            input.Climb = 1f;
            Step();
            Assert.IsFalse(rig.Cat.IsClimbing, "standing on the ceiling while pushing into it lets go");
        }
    }
}
