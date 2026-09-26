using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    // PAX-085 (D-087) §5 and R4: the inverter's pure timer. Fired in the room step at tick F, it inverts the motor steps of
    // room ticks F+1 .. F+duration; the cue is on from F through F+duration and blinks over the last 30.
    public sealed class ControlInversionTests
    {
        const int F = 40, D = 150;

        static ControlInversion Fired(int tick = F, int duration = D)
        {
            var inversion = new ControlInversion();
            inversion.Fire(tick, duration);
            return inversion;
        }

        [Test]
        public void BeforeAnyFire_NothingIsActive_AndNoCue()
        {
            var inversion = new ControlInversion();
            Assert.IsFalse(inversion.HasFired);
            for (int t = 0; t < 400; t++) { Assert.IsFalse(inversion.IsActive(t), "t" + t); Assert.IsFalse(inversion.IsCueVisible(t), "t" + t); }
            Assert.AreEqual(0, inversion.Remaining(0));
        }

        [Test]
        public void TheWindow_IsTheStepAfterTheFire_ThroughFirePlusDuration_ExactToTheTick()
        {
            ControlInversion inversion = Fired();
            Assert.IsTrue(inversion.HasFired);
            Assert.AreEqual(F, inversion.FireTick);
            Assert.IsFalse(inversion.IsActive(F), "the fire tick's own motor step ran before the fire");
            Assert.IsTrue(inversion.IsActive(F + 1), "first inverted step");
            Assert.IsTrue(inversion.IsActive(F + D), "last inverted step");
            Assert.IsFalse(inversion.IsActive(F + D + 1), "the step after the window");
            int active = 0;
            for (int t = 0; t < F + D + 100; t++) if (inversion.IsActive(t)) active++;
            Assert.AreEqual(D, active, "exactly DurationTicks inverted steps");
        }

        [Test]
        public void Remaining_CountsTheInvertedStepsStillToCome()
        {
            ControlInversion inversion = Fired();
            Assert.AreEqual(D, inversion.Remaining(F));
            Assert.AreEqual(D - 1, inversion.Remaining(F + 1));
            Assert.AreEqual(1, inversion.Remaining(F + D - 1));
            Assert.AreEqual(0, inversion.Remaining(F + D));
            Assert.AreEqual(0, inversion.Remaining(F + D + 10));
        }

        [Test]
        public void ARefireWhileActive_RestartsTheFullWindow()
        {
            ControlInversion inversion = Fired();
            const int again = F + 100;
            inversion.Fire(again, D);
            Assert.AreEqual(again, inversion.FireTick);
            Assert.IsTrue(inversion.IsActive(F + D + 1), "the old end no longer ends it");
            Assert.IsTrue(inversion.IsActive(again + D));
            Assert.IsFalse(inversion.IsActive(again + D + 1));
            Assert.AreEqual(D, inversion.Remaining(again));
        }

        [Test]
        public void Clear_EndsTheWindowAndTheCue()
        {
            ControlInversion inversion = Fired();
            inversion.Clear();
            Assert.IsFalse(inversion.HasFired);
            for (int t = F; t <= F + D + 1; t++) { Assert.IsFalse(inversion.IsActive(t), "t" + t); Assert.IsFalse(inversion.IsCueVisible(t), "t" + t); }
            Assert.AreEqual(0, inversion.Remaining(F + 1));
        }

        [Test]
        public void TheCue_IsOnFromTheFireTick_ThroughTheLastInvertedStep_BlinkingOverTheLastThirty()
        {
            ControlInversion inversion = Fired();
            Assert.IsFalse(inversion.IsCueVisible(F - 1));
            for (int t = F; t < F + D - 29; t++) Assert.IsTrue(inversion.IsCueVisible(t), "steady cue at t" + t);
            // The last 30 ticks of the window (F+D-29 .. F+D): off, on, off, on, off, on in runs of 5, ending on.
            var expected = new bool[30];
            for (int k = 0; k < 30; k++) expected[k] = (k / 5) % 2 == 1;
            for (int k = 0; k < 30; k++) Assert.AreEqual(expected[k], inversion.IsCueVisible(F + D - 29 + k), "blink tick " + k);
            Assert.IsTrue(inversion.IsCueVisible(F + D), "on at the last inverted step");
            Assert.IsFalse(inversion.IsCueVisible(F + D + 1), "off after the window");
            Assert.AreEqual(30, ControlInversion.BlinkTicks);
            Assert.AreEqual(150, ControlInversion.DefaultDurationTicks);
        }
    }
}
