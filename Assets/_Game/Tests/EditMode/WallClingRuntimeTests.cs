using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-105 (D-110 and its amendment): the wall branch in the PauseTestRig solo room (real ObserverSet,
    /// RoomManager, RoomDeath, CatInputRouter, LocalHumanDriver and CatMotor2D), like ClimbRuntimeTests. EditMode doesn't
    /// simulate physics here: the cat stays where it's put and each tick starts from zero velocity unless a test sets one.
    /// The rig has no floor, so the cat is airborne. Its collider is x [-0.5, 0.5], y [-0.58, -0.02]; the wall is a solid box
    /// x [0.52, 1.52], y [-3, 3] in the cat's reality, on its right, a grip wall (GripSurface) unless a test says otherwise.</summary>
    public sealed class WallClingRuntimeTests : PauseTestBase
    {
        const float FaceX = .52f;

        PauseTestRig rig;
        Scripted input;

        sealed class Scripted : ICatCommandSource
        {
            public float Move, Climb;
            public bool Jump, Grab;
            public CatCommand Read()
            {
                var c = new CatCommand { Move = Move, Climb = Climb, JumpPressed = Jump, JumpHeld = Jump, GrabPressed = Grab };
                Jump = false; Grab = false;
                return c;
            }
            public void ResetTransientState() { Jump = false; Grab = false; }
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
        GravityReceiver Gravity => rig.CatGo.GetComponent<GravityReceiver>();
        float G => Gravity.Strength;
        float JumpSpeed => JumpMath.SpeedForHeight(Config.JumpHeight, G);
        Collider2D CatCollider => System.Array.Find(rig.CatGo.GetComponents<Collider2D>(), c => !c.isTrigger);

        // A solid box in the cat's reality, `height` tall, its left face at x FaceX; with a body (kinematic) when asked.
        BoxCollider2D AddWall(float height = 6f, bool body = false, bool grip = true)
        {
            var go = new GameObject("Wall");
            go.transform.SetParent(rig.CatGo.transform.parent, false);
            go.transform.localPosition = new Vector2(FaceX + .5f, 0f);
            go.layer = rig.CatGo.layer;
            if (body) { Rigidbody2D rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic; }
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, height);
            if (grip) go.AddComponent<GripSurface>();
            Physics2D.SyncTransforms();
            return box;
        }

        Vector2 Step(Vector2 velocity = default)
        {
            rig.CatBody.linearVelocity = velocity;
            rig.Tick();
            Physics2D.SyncTransforms();
            return rig.CatBody.linearVelocity;
        }

        void Latch()
        {
            input.Grab = true;
            Step();
            Assert.IsTrue(rig.Cat.IsClinging, "precondition: latched");
        }

        [Test]
        public void TouchingAWall_WithoutGrab_TheMotorsOwnStep_Unchanged()
        {
            AddWall();
            Vector2 v = Step();
            Assert.IsFalse(rig.Cat.IsClinging);
            Assert.AreEqual(new Vector2(0f, -G * Dt), v, "the ordinary fall");
        }

        [Test]
        public void Grab_TouchingAWall_Latches_OntoTheFace_FacingIt_AndSlides()
        {
            AddWall();
            input.Grab = true;
            Vector2 v = Step();
            Assert.IsTrue(rig.Cat.IsClinging);
            Assert.AreEqual(1, rig.Cat.ClingSide, "the wall is on the right");
            Assert.IsTrue(rig.Cat.IsLatchMode);
            Assert.AreEqual(FaceX, CatCollider.bounds.max.x, .015f, "x snaps onto the face");
            Assert.AreEqual(new Vector2(0f, -G * Dt), v, "from rest: normal gravity, no sideways speed");
            Assert.AreEqual(new Vector2(0f, -Config.WallSlideSpeed), Step(new Vector2(0f, -15f)), "a fast fall is cut to the slide speed");
            Assert.AreEqual(2f, Config.WallSlideSpeed);
        }

        [Test]
        public void Grab_WhileRising_DoesntLatch_TheTopOfTheRiseDoes()
        {
            AddWall();
            input.Grab = true;
            Step(new Vector2(0f, 1f));
            Assert.IsFalse(rig.Cat.IsClinging, "rising");
            Step(new Vector2(0f, 0f));
            Assert.IsTrue(rig.Cat.IsClinging, "the press is remembered to the top of the rise");
        }

        [Test]
        public void AFaceUnderOneUnit_IsntLatched_OneUnitIs()
        {
            BoxCollider2D low = AddWall(height: .99f);
            input.Grab = true;
            Step();
            Assert.IsFalse(rig.Cat.IsClinging, "0.99 u");
            low.size = new Vector2(1f, 1f);
            Physics2D.SyncTransforms();
            Step();
            Assert.IsTrue(rig.Cat.IsClinging, "1.0 u (the press still remembered)");
        }

        // D-110 amendment 2: only a grip wall's collider (GripSurface) can be grabbed.
        [Test]
        public void APlainSolid_IsntLatched()
        {
            AddWall(grip: false);
            input.Grab = true;
            Vector2 v = Step();
            Assert.IsFalse(rig.Cat.IsClinging);
            Assert.AreEqual(new Vector2(0f, -G * Dt), v, "the ordinary fall");
        }

        [Test]
        public void ATriggerFace_IsntLatched()
        {
            AddWall().isTrigger = true;
            input.Grab = true;
            Step();
            Assert.IsFalse(rig.Cat.IsClinging);
        }

        [Test]
        public void GravityUp_NeverClings()
        {
            AddWall();
            Gravity.SetTargetDirection(Vector2.up);
            input.Grab = true;
            Step();
            Assert.IsFalse(rig.Cat.IsClinging);
            Assert.IsFalse(rig.Cat.IsLatchMode);
        }

        [Test]
        public void MoveTowardTheWall_DoesNothing_PushingAwayReleases_AndEndsLatchMode()
        {
            AddWall();
            Latch();
            input.Move = 1f;
            Assert.AreEqual(0f, Step().x, "toward the face");
            Assert.IsTrue(rig.Cat.IsClinging);
            input.Move = -.49f;
            Step();
            Assert.IsTrue(rig.Cat.IsClinging, "below the release threshold");
            input.Move = -.5f;
            Step();
            Assert.IsFalse(rig.Cat.IsClinging);
            Assert.IsFalse(rig.Cat.IsLatchMode);
            input.Move = 0f; input.Grab = true;
            Step();
            Assert.IsFalse(rig.Cat.IsClinging, "the face just left is locked until the cat lands");
        }

        [Test]
        public void WallJump_IsTheJumpLaunchPlusSideSpeedAway_NoCoyote_KeepsLatchMode()
        {
            AddWall();
            Latch();
            input.Jump = true;
            Vector2 v = Step();
            Assert.IsFalse(rig.Cat.IsClinging);
            Assert.IsTrue(rig.Cat.IsLatchMode);
            Assert.AreEqual(new Vector2(-Config.WallJumpSideSpeed, JumpSpeed), v);
            Assert.AreEqual(6f, Config.WallJumpSideSpeed);
            Assert.AreEqual(0f, PauseTestRig.GetPrivate<float>(rig.Cat, "coyoteTimer"));
            Assert.AreEqual(0f, PauseTestRig.GetPrivate<float>(rig.Cat, "jumpBufferTimer"));
        }

        [Test]
        public void AJumpBufferedBeforeTheLatch_WallJumpsOnTheLatchStep()
        {
            AddWall();
            input.Jump = true;
            Step();   // airborne, no coyote: the press waits in the buffer
            input.Grab = true;
            Vector2 v = Step();
            Assert.AreEqual(new Vector2(-Config.WallJumpSideSpeed, JumpSpeed), v);
            Assert.IsFalse(rig.Cat.IsClinging);
        }

        [Test]
        public void TheMoveLock_IgnoresMoveTowardTheWall_AndKeepsTheSideSpeed_ForEightSteps()
        {
            AddWall();
            Latch();
            input.Jump = true;
            Step();
            Assert.AreEqual(8, Config.WallJumpMoveLockTicks);
            Vector2 flying = new(-6f, 5f);
            for (int i = 0; i < 7; i++)
            {
                input.Move = i % 2 == 0 ? 1f : 0f;   // back toward the wall, or nothing
                Assert.AreEqual(-6f, Step(flying).x, 1e-5f, $"lock step {i + 1}");
            }
            input.Move = 1f;
            Assert.AreEqual(-6f, Step(flying).x, 1e-5f, "the eighth step");
            Assert.AreEqual(-6f + Config.Acceleration * Dt, Step(flying).x, 1e-4f, "after the lock: the ordinary acceleration");
        }

        [Test]
        public void ALaunch_AndAPush_LetGo_AndEndLatchMode()
        {
            AddWall();
            Latch();
            rig.Cat.ApplyLaunch(Vector2.up, 10f);
            Assert.IsFalse(rig.Cat.IsClinging, "launch");
            Assert.IsFalse(rig.Cat.IsLatchMode);
            rig.Cat.ResetMotion();
            Latch();
            rig.Cat.ApplyPush(new Vector2(-.01f, 0f));
            Assert.IsFalse(rig.Cat.IsClinging, "push");
            Assert.IsFalse(rig.Cat.IsLatchMode);
        }

        [Test]
        public void AFlip_LetsGo()
        {
            AddWall();
            Latch();
            Gravity.Flip();
            Step();
            Assert.IsFalse(rig.Cat.IsClinging);
            Assert.IsFalse(rig.Cat.IsLatchMode);
        }

        [Test]
        public void TheDeathHold_FreezesTheCling_AndTheResetClearsIt()
        {
            AddWall();
            Latch();
            Vector2 at = rig.CatBody.position;
            rig.Cat.Freeze();
            input.Move = -1f;
            Step();
            Assert.IsTrue(rig.Cat.IsClinging, "frozen as it is");
            Assert.AreEqual(at, rig.CatBody.position);
            rig.Cat.Unfreeze();
            rig.Cat.ResetMotion();
            Assert.IsFalse(rig.Cat.IsClinging);
            Assert.IsFalse(rig.Cat.IsLatchMode);
            input.Move = 0f; input.Grab = true;
            Step();
            Assert.IsTrue(rig.Cat.IsClinging, "a reset clears the face lock too");
        }

        [Test]
        public void ABodiedSolid_LatchesAfterAStillStep_AndLetsGoWhenItMoves()
        {
            BoxCollider2D wall = AddWall(body: true);
            input.Grab = true;
            Step();
            Assert.IsFalse(rig.Cat.IsClinging, "a solid with a body needs one still step first");
            Step();
            Assert.IsTrue(rig.Cat.IsClinging, "still since the last step");
            wall.attachedRigidbody.position += new Vector2(0f, .1f);
            wall.transform.position = wall.attachedRigidbody.position;
            Physics2D.SyncTransforms();
            Step();
            Assert.IsFalse(rig.Cat.IsClinging, "it moved");
            Assert.IsFalse(rig.Cat.IsLatchMode);
        }

        [Test]
        public void AStaticFace_SwitchedOff_LetsGo()
        {
            BoxCollider2D wall = AddWall();
            Latch();
            wall.enabled = false;
            Physics2D.SyncTransforms();
            Step();
            Assert.IsFalse(rig.Cat.IsClinging);
        }

        [Test]
        public void ClimbingAVine_WinsOverClinging()
        {
            AddWall();
            Latch();
            AddVine();
            input.Climb = 1f;
            Step();
            Assert.IsTrue(rig.Cat.IsClimbing);
            Assert.IsFalse(rig.Cat.IsClinging);
        }

        // ClimbRuntimeTests' vine, x [-0.1, 0.5], y [-1, 3], then the driver re-activated so it finds it.
        void AddVine()
        {
            var go = new GameObject("Vine");
            go.transform.SetParent(rig.CatGo.transform.parent, false);
            go.transform.localPosition = new Vector2(.2f, 1f);
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
            PauseTestRig.SetPrivate(v, "segments", new[] { segment });
            PauseTestRig.Invoke(v, "Awake");
            PauseTestRig.Invoke(v, "OnEnable");
            Physics2D.SyncTransforms();
            rig.Observer.SetDriver(new LocalHumanDriver(rig.Router));
        }
    }
}
