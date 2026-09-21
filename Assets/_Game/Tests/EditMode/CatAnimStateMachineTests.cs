using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests
{
    public class CatAnimStateMachineTests
    {
        const float RiseExit = 1f;
        const float FallEnter = 1f;
        const float WalkEnter = 0.15f;
        const float WalkExit = 0.1f;
        const float LandDuration = 0.12f;

        [Test]
        public void StartsIdle()
        {
            Assert.AreEqual(CatAnimState.Idle, Create().State);
        }

        [Test]
        public void AirborneAboveRiseThreshold_IsRise()
        {
            var machine = Create();

            CatAnimState state = machine.Step(false, 0f, -1.01f, 0.02f);

            Assert.AreEqual(CatAnimState.Rise, state);
        }

        [Test]
        public void AirborneAboveFallThreshold_IsFall()
        {
            var machine = Create();

            CatAnimState state = machine.Step(false, 0f, 1.01f, 0.02f);

            Assert.AreEqual(CatAnimState.Fall, state);
        }

        [Test]
        public void ApexBand_HoldsRiseUntilFallThresholdIsCrossed()
        {
            var machine = Create();
            machine.Step(false, 0f, -2f, 0.02f);

            Assert.AreEqual(CatAnimState.Rise, machine.Step(false, 0f, -1f, 0.02f));
            Assert.AreEqual(CatAnimState.Rise, machine.Step(false, 0f, 0f, 0.02f));
            Assert.AreEqual(CatAnimState.Rise, machine.Step(false, 0f, 1f, 0.02f));
            Assert.AreEqual(CatAnimState.Fall, machine.Step(false, 0f, 1.01f, 0.02f));
        }

        [Test]
        public void ApexBand_HoldsFallWithoutFlickeringBackToRise()
        {
            var machine = Create();
            machine.Step(false, 0f, 2f, 0.02f);

            Assert.AreEqual(CatAnimState.Fall, machine.Step(false, 0f, 0f, 0.02f));
            Assert.AreEqual(CatAnimState.Fall, machine.Step(false, 0f, -1f, 0.02f));
            Assert.AreEqual(CatAnimState.Rise, machine.Step(false, 0f, -1.01f, 0.02f));
        }

        [Test]
        public void FirstAirTickInsideApexBand_StartsFall()
        {
            var machine = Create();

            Assert.AreEqual(CatAnimState.Fall, machine.Step(false, 2f, -0.5f, 0.02f));
        }

        [Test]
        public void AirToGround_EntersLand()
        {
            var machine = Create();
            machine.Step(false, 0f, 2f, 0.02f);

            Assert.AreEqual(CatAnimState.Land, machine.Step(true, 0f, 0f, 0.02f));
        }

        [Test]
        public void Land_HoldsForConfiguredDuration()
        {
            var machine = Create();
            machine.Step(false, 0f, 2f, 0.02f);
            machine.Step(true, 0f, 0f, 0.02f);

            Assert.AreEqual(CatAnimState.Land, machine.Step(true, 0f, 0f, 0.06f));
            Assert.AreEqual(CatAnimState.Idle, machine.Step(true, 0f, 0f, 0.061f));
        }

        [Test]
        public void Land_IsNotInterruptedByMovement()
        {
            var machine = Create();
            machine.Step(false, 0f, 2f, 0.02f);
            machine.Step(true, 0f, 0f, 0.02f);

            Assert.AreEqual(CatAnimState.Land, machine.Step(true, 10f, 0f, 0.06f));
        }

        [Test]
        public void LeavingGround_InterruptsLandImmediately()
        {
            var machine = Create();
            machine.Step(false, 0f, 2f, 0.02f);
            machine.Step(true, 0f, 0f, 0.02f);

            Assert.AreEqual(CatAnimState.Rise, machine.Step(false, 0f, -2f, 0.02f));
        }

        [Test]
        public void GroundedSpeedAboveWalkEnter_EntersWalk()
        {
            var machine = Create();

            Assert.AreEqual(CatAnimState.Walk, machine.Step(true, 0.151f, 0f, 0.02f));
        }

        [Test]
        public void GroundedSpeedBelowWalkExit_EntersIdle()
        {
            var machine = Create();
            machine.Step(true, 1f, 0f, 0.02f);

            Assert.AreEqual(CatAnimState.Idle, machine.Step(true, 0.099f, 0f, 0.02f));
        }

        [Test]
        public void WalkHysteresis_HoldsCurrentGroundStateInsideBand()
        {
            var idleMachine = Create();
            var walkingMachine = Create();
            walkingMachine.Step(true, 1f, 0f, 0.02f);

            Assert.AreEqual(CatAnimState.Idle, idleMachine.Step(true, 0.125f, 0f, 0.02f));
            Assert.AreEqual(CatAnimState.Walk, walkingMachine.Step(true, 0.125f, 0f, 0.02f));
        }

        [Test]
        public void SurfaceSpeed_IsTreatedAsAbsolute()
        {
            var machine = Create();

            Assert.AreEqual(CatAnimState.Walk, machine.Step(true, -1f, 0f, 0.02f));
        }

        [Test]
        public void SameInputSequence_ProducesSameStates()
        {
            var first = Create();
            var second = Create();
            var inputs = new[]
            {
                new Input(true, 0f, 0f, 0.02f),
                new Input(true, 1f, 0f, 0.02f),
                new Input(false, 1f, -3f, 0.02f),
                new Input(false, 1f, 0f, 0.02f),
                new Input(false, 1f, 3f, 0.02f),
                new Input(true, 1f, 0f, 0.02f),
                new Input(true, 1f, 0f, 0.12f),
            };

            foreach (Input input in inputs)
            {
                CatAnimState firstState = first.Step(
                    input.Grounded,
                    input.SurfaceSpeed,
                    input.GravityVelocity,
                    input.Dt);
                CatAnimState secondState = second.Step(
                    input.Grounded,
                    input.SurfaceSpeed,
                    input.GravityVelocity,
                    input.Dt);

                Assert.AreEqual(firstState, secondState);
            }
        }

        [Test]
        public void Reset_ClearsLandingTimerAndUsesRequestedState()
        {
            var machine = Create();
            machine.Step(false, 0f, 2f, 0.02f);
            machine.Step(true, 0f, 0f, 0.02f);

            machine.Reset(CatAnimState.Walk);

            Assert.AreEqual(CatAnimState.Walk, machine.State);
            Assert.AreEqual(CatAnimState.Idle, machine.Step(true, 0f, 0f, 0.02f));
        }

        static CatAnimStateMachine Create() =>
            new CatAnimStateMachine(
                RiseExit,
                FallEnter,
                WalkEnter,
                WalkExit,
                LandDuration);

        readonly struct Input
        {
            public Input(bool grounded, float surfaceSpeed, float gravityVelocity, float dt)
            {
                Grounded = grounded;
                SurfaceSpeed = surfaceSpeed;
                GravityVelocity = gravityVelocity;
                Dt = dt;
            }

            public bool Grounded { get; }
            public float SurfaceSpeed { get; }
            public float GravityVelocity { get; }
            public float Dt { get; }
        }
    }
}
