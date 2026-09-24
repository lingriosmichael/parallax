using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Core;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-080 (D-080) §5.2: a betrayal route ends Dies (killer, cause, lead from revealedBy) or Recovers (the room
    // completes after revealedBy has visibly changed). Alive at a cap is a possible soft-lock (D-053).
    public sealed class BetrayalOutcomeTests
    {
        const string SoftLock = "possible soft-lock (D-053)";
        IDisposable session;
        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static object Fixture(string name, params object[] args) => Call(T("RouteFixtures"), name, args);
        static object Dies(string name, string killer, object route, string revealedBy) =>
            Activator.CreateInstance(T("Betrayal"), name, killer, DeathCause.Hazard, route, revealedBy);
        static object Recovers(string name, string revealedBy, object route) => Call(T("Betrayal"), "Recovers", name, revealedBy, route);

        (object result, List<string> errors) Check(object room, object betrayal)
        {
            var errors = new List<string>();
            bool recovers = F(betrayal, "Outcome").ToString() == "Recovers";
            object result = Call(T("RouteValidator"), recovers ? "CheckRecovery" : "CheckBetrayal", session, "FIX", room, betrayal, errors);
            return (result, errors);
        }

        [TestCase(false, true)]
        [TestCase(true, false)]    // seen red: the fake made solid never changes visibly, so the death has no reveal
        public void Dies_PassesWithTheKillerAndALeadFromRevealedBy(bool solid, bool passes)
        {
            (object lead, List<string> errors) = Check(Fixture("FakeOverPitRoom", solid), Dies("fake over the pit", "Pit_Hazard", Fixture("WalkRightUntilDead"), "Fake"));
            TestContext.Out.WriteLine(lead + "\n" + string.Join("\n", errors));
            Assert.AreEqual(passes, errors.Count == 0, lead + "\n" + string.Join("\n", errors));
            Assert.AreEqual("Pit_Hazard", F(lead, "Killer"), lead.ToString());
            if (passes) Assert.GreaterOrEqual((int)F(lead, "Lead"), 6, lead.ToString());
            else Assert.IsTrue(errors.Any(x => x.Contains("Fake never changes visibly")), string.Join("\n", errors));
        }

        [Test]
        public void Recovers_PassesWhenTheRoomCompletesAfterRevealedByChanges()
        {
            (object result, List<string> errors) = Check(Fixture("FakeOverFloorRoom", false), Recovers("fake over the floor", "Fake", Fixture("WalkRightToTheDoor")));
            CollectionAssert.IsEmpty(errors, result.ToString());
            Assert.IsTrue((bool)F(result, "Passed"), result.ToString());
            Assert.Less((int)F(result, "FirstVisibleTick"), (int)F(result, "CompletionTick"), result.ToString());
        }

        [Test]
        public void Recovers_FailsWhenRevealedByNeverChanges()   // seen red: the fake made solid
        {
            (object result, List<string> errors) = Check(Fixture("FakeOverFloorRoom", true), Recovers("solid ledge end", "Fake", Fixture("WalkRightToTheDoor")));
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.IsTrue((bool)F(result, "Completed"), "the fixture should still reach the door: " + result);
            Assert.IsFalse((bool)F(result, "Passed"), result.ToString());
            Assert.IsTrue(errors.Any(x => x.Contains("Fake never changes visibly")), string.Join("\n", errors));
        }

        [Test]
        public void Recovers_FailsOnADeath()   // seen red: the fake over a pit
        {
            (object result, List<string> errors) = Check(Fixture("FakeOverPitRoom", false), Recovers("fake over the pit", "Fake", Fixture("WalkRightToTheDoor")));
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.IsTrue((bool)F(result, "Died"), result.ToString());
            Assert.IsTrue(errors.Any(x => x.Contains("should recover but dies")), string.Join("\n", errors));
        }

        [Test]
        public void Recovers_RequiresRevealedBy() =>
            Assert.Throws<ArgumentException>(() => Recovers("no reveal", null, Fixture("WalkRightToTheDoor")));

        [Test]
        public void Recovers_GoalIsRoomComplete_WhateverItsSourceRouteDeclares()
        {
            object source = Activator.CreateInstance(T("Route"), "goal dead", Call(T("R"), "Dead"), Array.CreateInstance(T("RouteStep"), 0));
            object betrayal = Recovers("goal", "Fake", source);
            Assert.AreEqual("RoomComplete", F(F(F(betrayal, "Route"), "Goal"), "Label"));
        }

        // ---------- alive at a cap (seen red on each fixture) ----------

        [TestCase("IdleAtTheStepCap", "not reached within 600 ticks")]
        [TestCase("IdleAtTheTickCap", "tick cap 1500 reached")]
        public void ADiesBetrayalAliveAtACap_FailsAsAPossibleSoftLock(string route, string cap)
        {
            (object lead, List<string> errors) = Check(Call(T("RouteFixtures"), "FlatRoom"), Dies("idle", "Pit_Hazard", Fixture(route), "Pit_Hazard"));
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.IsTrue(errors.Any(x => x.Contains("does not die") && x.Contains(SoftLock) && x.Contains(cap)), string.Join("\n", errors));
        }

        [Test]
        public void ARecoversBetrayalAliveAtACap_FailsAsAPossibleSoftLock()
        {
            (object result, List<string> errors) = Check(Call(T("RouteFixtures"), "FlatRoom"), Recovers("idle", "Floor", Fixture("IdleAtTheStepCap")));
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.IsTrue(errors.Any(x => x.Contains("doesn't complete the room") && x.Contains(SoftLock)), string.Join("\n", errors));
        }

        [Test]
        public void ASolutionAliveAtTheTickCap_FailsAsAPossibleSoftLock()
        {
            object routes = Activator.CreateInstance(T("RoomRoutes"), Fixture("IdleAtTheTickCap"), Array.CreateInstance(T("Betrayal"), 0));
            object report = Call(T("RouteValidator"), "Run", session, "FIX", Call(T("RouteFixtures"), "FlatRoom"), routes);
            var errors = (List<string>)F(report, "Errors");
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.IsTrue(errors.Any(x => x.Contains("does not complete") && x.Contains(SoftLock) && x.Contains("tick cap 1500 reached")), string.Join("\n", errors));
        }

        [Test]
        public void ARoomsRecoversResults_GoToRecoveries_NotLeads()
        {
            object routes = Activator.CreateInstance(T("RoomRoutes"), Fixture("WalkRightToTheDoor"),
                new[] { Recovers("fake over the floor", "Fake", Fixture("WalkRightToTheDoor")) }.ToTypedArray(T("Betrayal")));
            object report = Call(T("RouteValidator"), "Run", session, "FIX", Fixture("FakeOverFloorRoom", false), routes);
            Assert.AreEqual(0, ((IList)F(report, "Leads")).Count);
            Assert.AreEqual(1, ((IList)F(report, "Recoveries")).Count);
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"));
        }
    }

    static class TypedArrays
    {
        public static Array ToTypedArray(this object[] items, Type elementType)
        {
            Array array = Array.CreateInstance(elementType, items.Length);
            for (int i = 0; i < items.Length; i++) array.SetValue(items[i], i);
            return array;
        }
    }
}
