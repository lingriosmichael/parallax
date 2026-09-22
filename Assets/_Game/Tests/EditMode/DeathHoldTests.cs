using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    public sealed class DeathHoldTests
    {
        [Test]
        public void Begin_HoldTicksH_StepsHoldingThenResetNowAtTplusH()
        {
            var hold = new DeathHold(3);

            Assert.AreEqual(DeathHoldPhase.Holding, hold.Begin());
            Assert.IsTrue(hold.IsHolding);

            Assert.AreEqual(DeathHoldPhase.Holding, hold.Step()); // T+1
            Assert.IsTrue(hold.IsHolding);
            Assert.AreEqual(DeathHoldPhase.Holding, hold.Step()); // T+2
            Assert.IsTrue(hold.IsHolding);
            Assert.AreEqual(DeathHoldPhase.ResetNow, hold.Step()); // T+3 = T+H
            Assert.IsFalse(hold.IsHolding);
        }

        [Test]
        public void Begin_SecondBeginWhileHoldingIsIgnored()
        {
            var hold = new DeathHold(5);
            hold.Begin();
            hold.Step(); // remaining now 4

            DeathHoldPhase reBegin = hold.Begin();

            Assert.AreEqual(DeathHoldPhase.Holding, reBegin);
            Assert.IsTrue(hold.IsHolding);
            // The countdown must not have been restarted: four more Step()s finish it.
            hold.Step(); hold.Step(); hold.Step();
            Assert.IsTrue(hold.IsHolding);
            Assert.AreEqual(DeathHoldPhase.ResetNow, hold.Step());
        }

        [Test]
        public void Begin_HoldTicksZero_ResetsImmediately()
        {
            var hold = new DeathHold(0);

            Assert.AreEqual(DeathHoldPhase.ResetNow, hold.Begin());
            Assert.IsFalse(hold.IsHolding);
        }

        [Test]
        public void Step_WhileNotHolding_ReportsLive()
        {
            var hold = new DeathHold(3);
            Assert.AreEqual(DeathHoldPhase.Live, hold.Step());
        }

        [Test]
        public void NegativeHoldTicks_ClampsToZero()
        {
            var hold = new DeathHold(-5);
            Assert.AreEqual(DeathHoldPhase.ResetNow, hold.Begin());
        }

        // ---------- §8.2 Freeze, at the pure level ----------

        [Test]
        public void Hold_RoomLifeTickAndTrapTimingDoNotStepWhileHolding()
        {
            // Models the intended RoomManager.OnStepped algorithm directly against DeathHold
            // and a real TrapTiming: only step RoomLifeTick/traps while DeathHold reports Live,
            // i.e. before Begin() and again after the tick ResetNow was reported on.
            var hold = new DeathHold(2);
            // Delay 1: an overlap at tick 1 schedules a fire for tick 2, but does not fire on
            // the same tick it was first seen — so a still-pending fire exists when the hold
            // begins, and it must never be consumed while holding.
            var trap = new TrapTiming(TrapTriggerSource.Overlap, TrapRepeatMode.Once, 1, 0, 1, 0);
            int roomLifeTick = 0;
            var fireTicks = new List<int>();

            void LiveTick()
            {
                roomLifeTick++;
                if (trap.Step(roomLifeTick, overlapping: true)) fireTicks.Add(roomLifeTick);
            }

            LiveTick(); // roomLifeTick 1: this is the tick the kill happens on
            Assert.AreEqual(1, roomLifeTick);
            CollectionAssert.IsEmpty(fireTicks, "sanity: the trap has not fired yet, only scheduled for tick 2");

            Assert.AreEqual(DeathHoldPhase.Holding, hold.Begin());

            // Two held ticks follow. LiveTick() is deliberately never called for them — that is
            // the property under test: the algorithm must not call it while holding, so
            // roomLifeTick can't advance and the trap's pending fire (due at tick 2) can't be
            // consumed either.
            Assert.AreEqual(DeathHoldPhase.Holding, hold.Step());
            Assert.AreEqual(DeathHoldPhase.ResetNow, hold.Step());

            Assert.AreEqual(1, roomLifeTick, "RoomLifeTick must not have advanced during the hold");
            CollectionAssert.IsEmpty(fireTicks, "the trap's pending fire must never have been consumed — LiveTick() was never called while holding");
        }

        [Test]
        public void Hold_ResetClearsChainFireDueDuringHold()
        {
            // A chain source fires once, at roomLifeTick 2. Its target (delay 1) would be due
            // at tick 3, but tick 3 falls inside a hold, so the target's Step() is never called
            // for it (the trap phase is skipped while holding, per RoomManager.OnStepped).
            var source = new TrapTiming(TrapTriggerSource.Overlap, TrapRepeatMode.Once, 0, 0, 1, 0);
            var target = new TrapTiming(TrapTriggerSource.Chain, TrapRepeatMode.Once, 1, 0, 1, 0);

            Assert.IsTrue(source.Step(2, overlapping: true)); // source fires at tick 2, due at target tick 3

            // A room reset restores every trap in the room together (RoomResetRegistry), not
            // just the one that was about to fire — so both the source and the target that
            // never got to consume it are reset.
            source.Reset();
            target.Reset();

            // Live resumes at RoomLifeTick 0. The target must not fire from the stale source
            // fire it never got to consume — Reset() clears the pending/seenSourceTick state
            // on both sides, so there is nothing left to consume.
            Assert.AreEqual(-1, source.LatestFireTick);
            Assert.IsFalse(target.Step(0, overlapping: false, sourceFireTick: source.LatestFireTick));
            Assert.IsFalse(target.Step(1, overlapping: false, sourceFireTick: source.LatestFireTick));
            Assert.AreEqual(-1, target.LatestFireTick);
        }
    }
}
