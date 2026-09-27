using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    // PAX-090 (D-091): the pure parts of checkpoint sections: TrapTiming's and TrapCountdown's capture and restore, and
    // SectionProgress (gates in order, per-section deaths, the PARALLAX_SECTIONS line).
    public sealed class CheckpointSectionCoreTests
    {
        // A fixed, irregular overlap pattern and a chain source that fires now and then.
        static bool Overlap(int t) => t % 37 < 9 || t % 53 == 20;
        static int Source(int t) => t < 15 ? -1 : t < 90 ? 15 : t < 170 ? 90 : 170;

        static TrapTiming Make(TrapTriggerSource source, TrapRepeatMode repeat) => new(source, repeat, 7, 30, 45, 11);

        static void AssertSame(TrapTiming a, TrapTiming b, int t)
        {
            Assert.AreEqual(a.LatestFireTick, b.LatestFireTick, $"t{t} fire");
            Assert.AreEqual(a.IsArmed, b.IsArmed, $"t{t} armed");
            Assert.AreEqual(a.JustRearmed, b.JustRearmed, $"t{t} rearmed");
            Assert.AreEqual(a.Capture(), b.Capture(), $"t{t} state");
        }

        [TestCase(TrapTriggerSource.Overlap, TrapRepeatMode.Once)]
        [TestCase(TrapTriggerSource.Overlap, TrapRepeatMode.Rearm)]
        [TestCase(TrapTriggerSource.Overlap, TrapRepeatMode.Periodic)]
        [TestCase(TrapTriggerSource.Chain, TrapRepeatMode.Once)]
        [TestCase(TrapTriggerSource.Chain, TrapRepeatMode.Rearm)]
        public void TrapTiming_RestoredCopy_ContinuesExactlyAsTheOriginal(TrapTriggerSource source, TrapRepeatMode repeat)
        {
            for (int at = 0; at < 200; at += 13)
            {
                TrapTiming original = Make(source, repeat), copy = Make(source, repeat);
                for (int t = 0; t <= at; t++) original.Step(t, Overlap(t), Source(t));
                // The copy has lived a different life before the restore.
                for (int t = 0; t <= at + 40; t++) copy.Step(t, !Overlap(t), Source(t) + 1);
                copy.Restore(original.Capture());
                AssertSame(original, copy, at);
                for (int t = at + 1; t < 260; t++)
                {
                    Assert.AreEqual(original.Step(t, Overlap(t), Source(t)), copy.Step(t, Overlap(t), Source(t)), $"restored at {at}, t{t} fired");
                    AssertSame(original, copy, t);
                }
            }
        }

        [Test]
        public void TrapTiming_Restore_UndoesEverythingAfterTheCapture()
        {
            TrapTiming timing = Make(TrapTriggerSource.Overlap, TrapRepeatMode.Rearm);
            for (int t = 0; t <= 5; t++) timing.Step(t, true);   // pending: fires at 0 + 7
            TrapTimingState atFive = timing.Capture();
            Assert.AreEqual(-1, timing.LatestFireTick, "precondition: pending, not fired");
            for (int t = 6; t < 120; t++) timing.Step(t, Overlap(t));
            Assert.AreNotEqual(atFive, timing.Capture(), "precondition: it moved on");
            timing.Restore(atFive);
            Assert.AreEqual(atFive, timing.Capture());
            Assert.IsFalse(timing.Step(6, false));
            Assert.IsTrue(timing.Step(7, false), "the pending fire survives the restore");
        }

        [Test]
        public void TrapCountdown_RestoredCopy_ContinuesExactlyAsTheOriginal()
        {
            for (int at = 0; at < 80; at += 7)
            {
                var original = new TrapCountdown(6, rearmOnExit: true);
                var copy = new TrapCountdown(6, rearmOnExit: true);
                for (int t = 0; t <= at; t++) original.Step(Overlap(t));
                for (int t = 0; t <= at + 20; t++) copy.Step(!Overlap(t));
                copy.Restore(original.Capture());
                Assert.AreEqual(original.Capture(), copy.Capture());
                for (int t = at + 1; t < 150; t++)
                {
                    Assert.AreEqual(original.Step(Overlap(t)), copy.Step(Overlap(t)), $"restored at {at}, t{t}");
                    Assert.AreEqual(original.State, copy.State);
                    Assert.AreEqual(original.TicksSinceFired, copy.TicksSinceFired);
                }
            }
        }

        [Test]
        public void SectionProgress_GatesPassInOrderOnly()
        {
            var p = new SectionProgress();
            p.Begin(3);
            Assert.AreEqual(0, p.Current);
            Assert.IsFalse(p.HasCheckpoint);
            Assert.IsFalse(p.TryEnter(2), "gate 2 is ignored until gate 1 is passed");
            Assert.IsFalse(p.TryEnter(0));
            Assert.IsTrue(p.TryEnter(1));
            Assert.IsTrue(p.HasCheckpoint);
            Assert.IsFalse(p.TryEnter(1), "once");
            Assert.IsTrue(p.TryEnter(2));
            Assert.IsFalse(p.TryEnter(3), "no section 3");
            Assert.AreEqual(2, p.Current);
            p.Begin(0);
            Assert.AreEqual(0, p.Current);
            Assert.IsFalse(p.TryEnter(1), "a room with no sections has no gates");
        }

        [Test]
        public void SectionProgress_CountsDeathsInTheCurrentSection_AndFormatsTheLine()
        {
            var p = new SectionProgress();
            p.Begin(3);
            p.RecordDeath();
            p.RecordDeath();
            p.TryEnter(1);
            p.RecordDeath();
            Assert.AreEqual(2, p.DeathsIn(0));
            Assert.AreEqual(1, p.DeathsIn(1));
            Assert.AreEqual(0, p.DeathsIn(2));
            Assert.AreEqual("PARALLAX_SECTIONS room=11 Start=2 Gate_2=1 Gate_3=0 total=3", p.Format(11, new[] { "Start", "Gate_2", "Gate_3" }));
            var none = new SectionProgress();
            none.Begin(0);
            none.RecordDeath();
            Assert.AreEqual(0, none.DeathsIn(0), "no sections: nothing counted here (RoomDeath keeps the room's count)");
        }
    }
}
