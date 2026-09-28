using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-060 (L014/L015 precondition), D-091 + D-095: a checkpoint rewind restores a shrinker's width and a mover's position,
    // direction and phase. RouteValidator.CheckRewind compares the elements at the rewind tick only, and two rewinds with each
    // other; this compares every tick after the rewind with a run that never died, at the same room tick, so a mover back
    // at the right place but heading the wrong way, or a shrinker back at full width, fails.
    public sealed class MovingFloorRewindTests
    {
        static readonly Type Fixtures = Type.GetType("Parallax.Editor.Setup.MovingFloorFixtures, Parallax.Editor");
        IDisposable session;
        object reference, rewound;

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            object routeCase = Call(Fixtures, "RewindReference");
            reference = Replay(session, routeCase);
            rewound = ReplayRoute(session, F(routeCase, "Room"), Call(Fixtures, "RewindAfter", 60));
        }

        [OneTimeTearDown] public void Close() => session?.Dispose();

        static List<object> Raw(object replay) => ((IEnumerable)F(replay, "Records")).Cast<object>().ToList();

        static int RewindIndex(List<object> records)
        {
            for (int i = 1; i < records.Count; i++) if ((bool)F(records[i - 1], "Holding") && !(bool)F(records[i], "Holding")) return i;
            return -1;
        }

        // Every record after the rewind against the reference record at the same room tick: the element's drawn signature (pose
        // and size) and its fire tick. Returns the ticks compared; fails on the first difference.
        int AssertSameAfterRewind(string element)
        {
            List<object> a = Raw(rewound), r = Raw(reference);
            int ea = ElementIndex(rewound, element), er = ElementIndex(reference, element);
            Assert.GreaterOrEqual(ea, 0, $"{element} not in the rewound replay"); Assert.GreaterOrEqual(er, 0, $"{element} not in the reference");
            int start = RewindIndex(a);
            Assert.Greater(start, 0, "no rewind happened\n" + Dump(rewound));
            var byRoomTick = new Dictionary<int, object>();
            foreach (object rec in r) byRoomTick[(int)F(rec, "RoomLifeTick")] = rec;
            int compared = 0;
            for (int i = start; i < a.Count; i++)
            {
                int roomTick = (int)F(a[i], "RoomLifeTick");
                Assert.IsTrue(byRoomTick.TryGetValue(roomTick, out object x), $"the reference never reached room tick {roomTick}");
                int fa = ((int[])F(a[i], "FireTick"))[ea], fr = ((int[])F(x, "FireTick"))[er];
                int sa = ((int[])F(a[i], "Signature"))[ea], sr = ((int[])F(x, "Signature"))[er];
                Assert.AreEqual(sr, sa, $"{element}'s pose/size at room tick {roomTick}, {i - start} ticks after the rewind");
                Assert.AreEqual(fr, fa, $"{element}'s fire tick at room tick {roomTick}, {i - start} ticks after the rewind");
                compared++;
            }
            return compared;
        }

        [Test]
        public void TheRewind_PutsTheRoomClockBackAtTheGate()
        {
            List<object> a = Raw(rewound);
            int start = RewindIndex(a);
            Assert.Greater(start, 0, "no rewind happened");
            int gate = (int)F(a[start], "RoomLifeTick"), died = (int)F(a[start - 1], "RoomLifeTick");
            Assert.Less(gate, died, "the rewind should take the room clock back to the gate");
        }

        // Mover's cycle (move 60, hold 20, back 60, every 200): the gate falls in its hold; 260 ticks after the rewind cover its
        // return, its rearm and its next trip out, so a wrong direction or phase shows.
        [Test]
        public void AfterARewind_TheMoversPoseDirectionAndPhase_MatchARunThatNeverDied() =>
            Assert.GreaterOrEqual(AssertSameAfterRewind("Mover"), 200);

        // Shrink (300 ticks) is mid-shrink at the gate and still shrinking for the whole comparison.
        [Test]
        public void AfterARewind_TheShrinkersWidth_MatchesARunThatNeverDied()
        {
            List<object> a = Raw(rewound);
            int start = RewindIndex(a), gate = (int)F(a[start], "RoomLifeTick");
            object atGate = Raw(reference).First(r => (int)F(r, "RoomLifeTick") == gate);
            int fire = ((int[])F(atGate, "FireTick"))[ElementIndex(reference, "Shrink")];
            Assert.IsTrue(fire >= 0 && gate - fire > 0 && gate - fire < 300, $"Shrink should be mid-shrink at the gate (fire t{fire}, gate t{gate})");
            Assert.GreaterOrEqual(AssertSameAfterRewind("Shrink"), 200);
        }
    }
}
