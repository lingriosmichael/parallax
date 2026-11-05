using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    /// <summary>D-324 (PAX-129): the air jump and the whisker dodge, on CatSkills and through the real CatMotor2D on a real floor
    /// (CatMotor2DJumpWindowTests' rig: EditMode simulates no physics, so a walk-off or a landing is a teleport).</summary>
    public sealed class CatSkillsTests
    {
        const float CatHalfHeight = .28f;
        static readonly Vector2 OnFloor = new Vector2(0f, CatHalfHeight + .01f);
        static readonly Vector2 OffTheEdge = new Vector2(20f, CatHalfHeight + .01f);
        const float Dt = .02f;

        [SetUp] public void FixedRate() => TickTime.SecondsPerTickSource = () => Dt;
        [TearDown] public void TearDown() => TickTime.ResetToDefault();

        // ---------- CatSkills ----------

        [Test]
        public void AirJump_OnlyInTheAir_WithAJumpLeft_NotAboutToLand_AndNotOverAGroundJump()
        {
            Assert.IsTrue(CatSkills.AirJump(true, false, false, false, 1));
            Assert.IsFalse(CatSkills.AirJump(false, false, false, false, 1), "no press");
            Assert.IsFalse(CatSkills.AirJump(true, true, false, false, 1), "the coyote jump fired");
            Assert.IsFalse(CatSkills.AirJump(true, false, true, false, 1), "grounded");
            Assert.IsFalse(CatSkills.AirJump(true, false, false, true, 1), "about to land: the buffer's");
            Assert.IsFalse(CatSkills.AirJump(true, false, false, false, 0), "spent");
        }

        [Test]
        public void Dodge_LastsItsTicks_AndTheNextPressCountsAfterTheCooldown()
        {
            int active = 0, cooldown = 0;
            Assert.IsTrue(CatSkills.Dodge(ref active, ref cooldown, true, 15, 250));
            int dodging = 1, refused = 0;
            for (int t = 1; t < 250; t++)
            {
                if (CatSkills.Dodge(ref active, ref cooldown, true, 15, 250)) Assert.Fail($"a press {t} ticks after a dodge started another");
                if (active > 0) dodging++;
                refused++;
            }
            Assert.AreEqual(15, dodging, "the dodge covers 15 ticks");
            Assert.AreEqual(249, refused);
            Assert.AreEqual(1f - 1f / 250f, CatSkills.DodgeReadiness(cooldown, 250), 1e-5f, "one step short of ready");
            Assert.IsTrue(CatSkills.Dodge(ref active, ref cooldown, true, 15, 250), "250 ticks after the last, a press dodges again");
            Assert.AreEqual(0f, CatSkills.DodgeReadiness(cooldown, 250), 1e-5f, "just used");
        }

        // ---------- Through the motor ----------

        sealed class Rig
        {
            public GameObject Root, CatGo;
            public CatMotor2D Motor;
            public Rigidbody2D Body;

            public bool Step(bool jump = false, bool whisker = false)
            {
                var cmd = new CatCommand { JumpPressed = jump, WhiskerPressed = whisker };
                Motor.Step(in cmd, Dt);
                return Vector2.Dot(Body.linearVelocity, Vector2.up) > 0f;
            }

            public void MoveTo(Vector2 p) { CatGo.transform.position = p; Physics2D.SyncTransforms(); }
            public void Dispose() => Object.DestroyImmediate(Root);
        }

        static Rig Build()
        {
            var rig = new Rig { Root = new GameObject("RealityRoot_A") };
            RealityRoot reality = rig.Root.AddComponent<RealityRoot>();
            CatMotor2DFreezeTests.SetPrivate(reality, "id", ObserverId.A);
            CatMotor2DFreezeTests.Invoke(reality, "Awake");
            int layer = LayerMask.NameToLayer("RealityA");
            var floor = new GameObject("Floor");
            floor.transform.SetParent(rig.Root.transform, false);
            floor.layer = layer;
            floor.transform.position = new Vector2(0f, -.5f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(10f, 1f);
            rig.CatGo = new GameObject("Cat");
            rig.CatGo.transform.SetParent(rig.Root.transform, false);
            rig.CatGo.layer = layer;
            rig.Body = rig.CatGo.AddComponent<Rigidbody2D>();
            rig.CatGo.AddComponent<BoxCollider2D>().size = new Vector2(1f, CatHalfHeight * 2f);
            CatMotor2DFreezeTests.Invoke(rig.CatGo.AddComponent<GravityReceiver>(), "Awake");
            rig.Motor = rig.CatGo.AddComponent<CatMotor2D>();
            var config = ScriptableObject.CreateInstance<CatMotorConfig>();
            CatMotor2DFreezeTests.SetPrivate(config, "airJumps", 1);   // D-324 amendment 2: off in the game, kept testable
            CatMotor2DFreezeTests.SetPrivate(rig.Motor, "config", config);
            CatMotor2DFreezeTests.Invoke(rig.Motor, "Awake");
            rig.MoveTo(OnFloor);
            return rig;
        }

        [Test]
        public void AirJump_OncePerJump_ToItsHeight_AndBackAfterLanding()
        {
            Rig rig = Build();
            try
            {
                rig.Step();
                Assert.IsTrue(rig.Motor.IsGrounded, "rig: the cat stands on the floor");
                rig.MoveTo(OffTheEdge);
                for (int i = 0; i < 10; i++) Assert.IsFalse(rig.Step(), "falling");

                Assert.IsTrue(rig.Step(jump: true), "a press in the air, coyote closed, jumps");
                Assert.IsTrue(rig.Motor.AirJumpedThisStep && !rig.Motor.JumpedThisStep);
                float rise = Vector2.Dot(rig.Body.linearVelocity, Vector2.up);
                Assert.AreEqual(JumpMath.SpeedForHeight(1f, rig.CatGo.GetComponent<GravityReceiver>().Strength), rise, 1e-3f, "to the air jump's 1 u");

                for (int i = 0; i < 3; i++) rig.Step();
                rig.Step(jump: true);
                Assert.IsFalse(rig.Motor.AirJumpedThisStep, "one air jump per jump");

                rig.Body.linearVelocity = Vector2.zero;   // come down (EditMode moves nothing)
                for (int i = 0; i < 7; i++) rig.Step();   // past the press's buffer
                rig.MoveTo(OnFloor);
                rig.Step();
                Assert.IsTrue(rig.Motor.IsGrounded, "rig: landed");
                rig.MoveTo(OffTheEdge);
                for (int i = 0; i < 10; i++) rig.Step();
                Assert.IsTrue(rig.Step(jump: true) && rig.Motor.AirJumpedThisStep, "landing gives the air jump back");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void AirJump_APressJustBeforeLanding_IsTheBufferedJumpOnLanding()
        {
            Rig rig = Build();
            try
            {
                rig.Step();
                rig.MoveTo(OffTheEdge);
                for (int i = 0; i < 10; i++) rig.Step();   // falling at 6 u/s: the buffer's 0.12 s covers 0.72 u
                rig.MoveTo(OnFloor + Vector2.up * .4f);
                rig.Step(jump: true);
                Assert.IsFalse(rig.Motor.IsGrounded, "rig: still in the air");
                Assert.IsFalse(rig.Motor.AirJumpedThisStep, "0.4 u above the floor the press is left to the buffer");
                rig.MoveTo(OnFloor);
                Assert.IsTrue(rig.Step(), "and jumps on landing");
                Assert.IsTrue(rig.Motor.JumpedThisStep);
            }
            finally { rig.Dispose(); }
        }

        // Off the edge, falling, the coyote jump long gone.
        static void FallOffTheEdge(Rig rig)
        {
            rig.Step();
            rig.MoveTo(OffTheEdge);
            for (int i = 0; i < 10; i++) rig.Step();
        }

        [Test]
        public void AirJump_NearTheApex_JustOverAFloor_IsLeftToTheBuffer()
        {
            Rig rig = Build();
            try
            {
                FallOffTheEdge(rig);
                // Still at the apex 0.15 u over the floor: gravity alone brings the cat down within the buffer.
                rig.MoveTo(OnFloor + Vector2.up * .15f);
                rig.Body.linearVelocity = Vector2.zero;
                rig.Step(jump: true);
                Assert.IsFalse(rig.Motor.AirJumpedThisStep, "gravity's pull counts: the press is the landing's");
                rig.Body.linearVelocity = Vector2.zero;
                rig.MoveTo(OnFloor);
                Assert.IsTrue(rig.Step() && rig.Motor.JumpedThisStep, "the full jump on landing");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void AirJump_SlidingPastALedgeCorner_IsNotLanding()
        {
            Rig rig = Build();
            try
            {
                FallOffTheEdge(rig);
                // Over the floor's east corner (x 5) 0.3 u up, falling at 3 u/s and running east at 6: straight down the floor is
                // in reach, but by the time the cat comes down it's past the corner.
                rig.MoveTo(new Vector2(5.2f, OnFloor.y + .3f));
                rig.Body.linearVelocity = new Vector2(6f, -3f);
                Assert.IsTrue(rig.Step(jump: true), "the press jumps");
                Assert.IsTrue(rig.Motor.AirJumpedThisStep);
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void AirJump_APressLeftToALandingThatNeverCame_JumpsOnTheBuffersLastStep()
        {
            Rig rig = Build();
            try
            {
                FallOffTheEdge(rig);
                rig.MoveTo(OnFloor + Vector2.up * .3f);
                rig.Body.linearVelocity = new Vector2(0f, -3f);
                rig.Step(jump: true);
                Assert.IsFalse(rig.Motor.AirJumpedThisStep, "about to land: left to the buffer");
                rig.MoveTo(new Vector2(OffTheEdge.x, OnFloor.y + .3f));   // the floor went away
                int buffer = TickTime.ToWholeTicks(.12f);
                for (int i = 1; i < buffer - 1; i++) { rig.Step(); Assert.IsFalse(rig.Motor.AirJumpedThisStep, $"step {i}: the buffer's still open"); }
                Assert.IsTrue(rig.Step() && rig.Motor.AirJumpedThisStep, "the press isn't lost: the air jump on the buffer's last step");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void AirJump_ItsBuffer_IsNoJumpOnLanding()
        {
            Rig rig = Build();
            try
            {
                FallOffTheEdge(rig);
                Assert.IsTrue(rig.Step(jump: true) && rig.Motor.AirJumpedThisStep);
                // A low ceiling sends the cat straight back down onto the floor within the press's buffer.
                rig.Body.linearVelocity = Vector2.zero;
                rig.MoveTo(OnFloor);
                Assert.IsFalse(rig.Step(), "no second jump from the same press");
                Assert.IsTrue(rig.Motor.IsGrounded && !rig.Motor.JumpedThisStep);
                Assert.IsTrue(rig.Step(jump: true) && rig.Motor.JumpedThisStep, "a new press jumps");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void Dodge_ThroughTheMotor_LastsAndRefills_AndARespawnMakesItReady()
        {
            Rig rig = Build();
            try
            {
                rig.Step(whisker: true);
                Assert.IsTrue(rig.Motor.IsDodging && rig.Motor.DodgedThisStep);
                Assert.Less(rig.Motor.DodgeReadiness, .01f, "just used");
                for (int i = 0; i < 100; i++) rig.Step(whisker: true);
                Assert.IsFalse(rig.Motor.IsDodging, "a press during the cooldown does nothing");
                Assert.AreEqual(100f / 250f, rig.Motor.DodgeReadiness, .01f, "2 s into the 5 s cooldown");
                rig.Motor.ResetMotion();
                Assert.AreEqual(1f, rig.Motor.DodgeReadiness, "a respawn makes it ready");
                rig.Step(whisker: true);
                Assert.IsTrue(rig.Motor.IsDodging);
            }
            finally { rig.Dispose(); }
        }
    }
}
