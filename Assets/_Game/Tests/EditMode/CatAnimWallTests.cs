using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    // PAX-105 (D-110, §5.2): the state machine's wall rows (CatAnimStateMachine.Wall.cs), with CatAnimStateMachineTests' settings
    // (MinStateFrames 2, the climb's still speed and leap length). Clinging faces the wall (the paws toward it): WallCling still,
    // WallSlide sliding, and a stop never flashes WallCling; a wall jump shows WallJump facing away, then the air rows, and a
    // landing ends it.
    public sealed class CatAnimWallTests
    {
        const float RiseExit = 1f, FallEnter = 1f, WalkEnter = .15f, WalkExit = .1f, LandDuration = .12f;
        const float RunEnter = 2.4f, RunExit = 1.8f, TurnDuration = .1f, FlipHysteresis = .05f;
        const float HardLandDistance = 2.75f, TakeOffDuration = 2f / 60f, HardLandDuration = 3f / 12f, AirGrace = .03f;
        const float LeapMin = 6.9f, LeapMax = 12.7f, LeapDuration = .27f, ClimbStill = .02f, F = 1f / 60f;

        static CatAnimStateMachine Create() =>
            new CatAnimStateMachine(new CatAnimSettings(RiseExit, FallEnter, WalkEnter, WalkExit, LandDuration,
                RunEnter, RunExit, TurnDuration, FlipHysteresis, snapAcceleration: 20f, minStateFrames: 2,
                hardLandDistance: HardLandDistance, takeOffDuration: TakeOffDuration, hardLandDuration: HardLandDuration, airGraceDrop: AirGrace,
                speedLeadTolerance: .5f, climbStillSpeed: ClimbStill, leapSpeedMin: LeapMin, leapSpeedMax: LeapMax, leapDuration: LeapDuration));

        // One frame on a wall on `side`, falling at `fall` u/s (0: hanging still).
        static CatAnimState Wall(CatAnimStateMachine m, int side, float fall, float height = 2f) =>
            m.Step(new CatAnimInput(false, false, 0f, fall, F, false, -1f, height, bodySurfaceSpeed: 0f, bodyVelocityAlongGravity: fall,
                clinging: true, clingSide: side));

        static CatAnimState Air(CatAnimStateMachine m, float alongGravity, float surface, float height, bool wallJumped = false) =>
            m.Step(new CatAnimInput(false, false, surface, alongGravity, F, false, -1f, height, bodySurfaceSpeed: surface, bodyVelocityAlongGravity: alongGravity,
                wallJumped: wallJumped));

        static CatAnimState Ground(CatAnimStateMachine m, float height = 0f) =>
            m.Step(new CatAnimInput(true, false, 0f, 0f, F, false, -1f, height, bodySurfaceSpeed: 0f, bodyVelocityAlongGravity: 0f));

        static CatAnimStateMachine Clinging(int side)
        {
            CatAnimStateMachine m = Create();
            for (int i = 0; i < 3; i++) Ground(m);
            for (int i = 0; i < 10; i++) Air(m, -5f, 0f, 1f);
            for (int i = 0; i < 3; i++) Wall(m, side, 0f);
            return m;
        }

        // §14 G7: a wall jump on the frame after the latch holds the cling pose for MinStateFrames (2), then shows WallJump.
        [Test]
        public void AWallJumpRightAfterTheLatch_HoldsTheClingPose_ForMinStateFrames()
        {
            CatAnimStateMachine m = Create();
            for (int i = 0; i < 3; i++) Ground(m);
            for (int i = 0; i < 10; i++) Air(m, -5f, 0f, 1f);
            Assert.AreEqual(CatAnimState.WallSlide, Wall(m, 1, 1f), "the latch");
            Assert.AreEqual(CatAnimState.WallSlide, Air(m, -9.8f, -6f, 2.1f, wallJumped: true), "held: its second frame");
            Assert.AreEqual(CatAnimState.WallJump, Air(m, -9.3f, -6f, 2.25f, wallJumped: true), "then the wall jump");
            Assert.AreEqual(-1, m.Facing);
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void AGrab_ShowsWallCling_FacingTheWall(int side)
        {
            CatAnimStateMachine m = Clinging(side);
            Assert.AreEqual(CatAnimState.WallCling, m.State);
            Assert.AreEqual(side, m.Facing, "the paws toward the wall");
        }

        [Test]
        public void Sliding_ShowsWallSlide_AndAStopHoldsItForMinStateFrames()
        {
            CatAnimStateMachine m = Clinging(1);
            Assert.AreEqual(CatAnimState.WallSlide, Wall(m, 1, 2f));
            Assert.AreEqual(CatAnimState.WallSlide, Wall(m, 1, 2f));
            Assert.AreEqual(CatAnimState.WallSlide, Wall(m, 1, 0f), "the first still frame holds the slide (no flash)");
            Assert.AreEqual(CatAnimState.WallCling, Wall(m, 1, 0f));
            Assert.AreEqual(CatAnimState.WallSlide, Wall(m, 1, ClimbStill + .01f), "sliding again: at once");
        }

        [Test]
        public void AWallJump_ShowsWallJump_FacingAway_ThenTheAirRows_AndALandingEndsIt()
        {
            CatAnimStateMachine m = Clinging(1);
            Assert.AreEqual(CatAnimState.WallJump, Air(m, -9.8f, -6f, 2.1f, wallJumped: true), "the jump's frame");
            Assert.AreEqual(-1, m.Facing, "away from the right wall");
            Assert.AreEqual(CatAnimState.WallJump, Air(m, -9.3f, -6f, 2.25f, wallJumped: true), "the flag held to the next tick: still one jump");
            int frames = 2;   // the WallJump frames shown
            while (Air(m, -8f, -6f, 2.4f) == CatAnimState.WallJump) { frames++; Assert.Less(frames, 60); }
            Assert.AreEqual(CatAnimState.Rise, m.State, "then the air rows");
            Assert.AreEqual((int)System.Math.Round(LeapDuration / F), frames, 1, $"for the leap's length ({frames} frames)");
            CatAnimStateMachine n = Clinging(-1);
            Air(n, -9.8f, 6f, 2.1f, wallJumped: true);
            Assert.AreEqual(1, n.Facing, "away from a left wall");
            Ground(n, 0f); Ground(n, 0f);
            Assert.That(n.State, Is.EqualTo(CatAnimState.Land).Or.EqualTo(CatAnimState.HardLand), "a landing during WallJump");
        }

        [Test]
        public void LettingGo_ShowsTheAirRows_AFall()
        {
            CatAnimStateMachine m = Clinging(1);
            Wall(m, 1, 2f);
            Assert.AreEqual(CatAnimState.Fall, Air(m, 2.6f, 0f, 1.9f));
        }

        // The slide isn't a fall: landing from a wall measures the drop from where the cat let go, not from the jump's top.
        [Test]
        public void SlidingToTheFloor_LandsSoftly()
        {
            CatAnimStateMachine m = Create();
            for (int i = 0; i < 3; i++) Ground(m);
            for (int i = 0; i < 10; i++) Air(m, -5f, 0f, 3f);
            for (float h = 3f; h > .1f; h -= 2f * F) Wall(m, 1, 2f, h);
            Air(m, 2f, 0f, .05f);
            Ground(m, 0f); Ground(m, 0f);
            Assert.AreEqual(CatAnimState.Land, m.State);
            Assert.Less(m.LastFallDistance, .5f);
        }
    }
}
