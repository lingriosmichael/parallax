using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-084 (D-086) §5, R1-R3: spears through the route harness (the real game code, with physics). Rooms and routes
    // are SpearFixtures' harness cases.
    public sealed class SpearHarnessTests
    {
        static readonly Type Fixtures = Type.GetType("Parallax.Editor.Setup.SpearFixtures, Parallax.Editor");
        const string DropShaft = "Spear_Drop_Shaft";
        const float ShaftTop = 1.5f;
        IDisposable session;

        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static object Spear(string fixture, params object[] args)
        {
            Assert.NotNull(Fixtures, "SpearFixtures not found");
            return Call(Fixtures, fixture, args);
        }

        // R1: the drop meets the 0.4 shaft at the 20 u/s cap and lands on it; standing, the cat rests on its top.
        [Test]
        public void ACatDroppedAtMaxFall_LandsOnAStuckSpear_AndStaysOnTop()
        {
            object replay = Replay(session, Spear("JumpOffStuckSpear"));
            List<Rec> records = Records(replay);
            int landed = records.FindIndex(r => r.Grounded && r.Ground == DropShaft);
            Assert.Greater(landed, 0, "never landed on the shaft\n" + Dump(replay));
            float fastest = records.Take(landed).Min(r => r.Vy);
            float bottomOffset = PrecisionTestApi.Motor().ColliderSize.y * .5f;
            TestContext.Out.WriteLine($"landed t{records[landed].Tick} at y {records[landed].Y - bottomOffset:F3} (paw), fastest fall {fastest:F2} u/s");
            Assert.LessOrEqual(fastest, -19.9f, "the drop never reached the max fall speed\n" + Dump(replay));
            foreach (Rec r in records.Skip(landed).TakeWhile(r => r.Grounded))
                Assert.AreEqual(ShaftTop, r.Y - bottomOffset, .02f, $"t{r.Tick}: resting on the shaft's top");
            Assert.IsFalse(records.Any(r => r.Dead), "nothing kills here\n" + Dump(replay));
        }

        // §5: a cat standing on a stuck spear jumps off it like off any floor (D-082's 1.6).
        [Test]
        public void ACatStandingOnAStuckSpear_JumpsOffIt()
        {
            object replay = Replay(session, Spear("JumpOffStuckSpear"));
            List<Rec> records = Records(replay);
            int jump = records.FindIndex(r => r.JumpPressed);
            Assert.Greater(jump, 0, "no jump\n" + Dump(replay));
            float standing = records[jump - 1].Y, apex = records.Skip(jump).Max(r => r.Y);
            TestContext.Out.WriteLine($"jumped t{records[jump].Tick}; apex {apex - standing:F3} above the standing pose");
            Assert.IsTrue(records[jump - 1].Grounded && records[jump - 1].Ground == DropShaft, "standing on the shaft before the jump");
            Assert.AreEqual(1.6f, apex - standing, .1f, "a normal jump from the shaft");
            Assert.IsTrue(records.Skip(jump + 1).Any(r => r.Grounded && r.Ground == DropShaft), "lands back on the shaft\n" + Dump(replay));
        }

        // R3, seen red: standing on the stuck Spear_Drop, the cat is hit by Spear_High. Before killer naming by lethal
        // ticks, the stuck spear (visible and touching) was a second candidate: an ambiguous kill.
        [Test]
        public void AHigherSpear_KillingACatOnAStuckSpear_IsTheOnlyKiller()
        {
            object replay = Replay(session, Spear("SecondSpearHitsCatOnStuckSpear"));
            object kill = F(replay, "Kill");
            Assert.NotNull(kill, "no kill\n" + Dump(replay));
            var candidates = (List<string>)F(kill, "Candidates");
            TestContext.Out.WriteLine($"killed t{F(kill, "Tick")}: {string.Join(", ", candidates)}");
            CollectionAssert.AreEqual(new[] { "Spear_High" }, candidates, Dump(replay));
        }

        // R2, seen red: over the sweep, exactly the jump that leaves the ground on the stop tick is caught by the stop + 1
        // check, named as the spear. Spear_Pin: tell 8, travel 14 - 3 = 11 at 1.2 u/tick = 10 flight ticks, so the stop
        // tick is s = 18 and the check s = 19. Every other kill in the sweep is a flight kill, also the spear's.
        [Test]
        public void AJumpIntoTheShaftDuringTheStopTick_DiesOnStopPlusOne_NamedAsTheSpear()
        {
            const int check = 8 + 10 + 1;
            var stopPlusOne = new List<int>();
            var log = new List<string>();
            for (int k = 0; k <= 30; k++)
            {
                object replay = Replay(session, Spear("JumpIntoTheShaftAfterTheFire", k));
                object kill = F(replay, "Kill");
                if (kill == null) { log.Add($"k {k}: survived"); continue; }
                List<Rec> records = Records(replay);
                int spear = ElementIndex(replay, "Spear_Pin");
                Rec at = records[(int)F(kill, "Tick")];
                int s = at.RoomLifeTick - at.FireTick[spear];
                log.Add($"k {k}: killed at s {s} by {F(kill, "Killer") ?? "ambiguous"}");
                Assert.AreEqual("Spear_Pin", F(kill, "Killer"), $"k {k}\n" + Dump(replay));
                Assert.LessOrEqual(s, check, $"k {k}: a stuck spear never kills\n" + Dump(replay));
                if (s == check) stopPlusOne.Add(k);
            }
            TestContext.Out.WriteLine(string.Join("\n", log));
            Assert.AreEqual(1, stopPlusOne.Count, "exactly one jump enters the shaft during the stop tick's physics step\n" + string.Join("\n", log));
        }
    }
}
