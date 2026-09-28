using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-060 (D-093): each band-2 content rule seen red on a synthetic room, routes or replay (Band2Fixtures, reached by
    // reflection like every Editor type). A section over 1000 ticks is D-091's rule, already red-tested in
    // CheckpointSectionValidatorTests; Band2LevelTests runs it on every band-2 level.
    public sealed class Band2RulesTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");

        static object Fixture(string name, params object[] args) => Call(T("Band2Fixtures"), name, args);

        static object V(string method, params object[] args)
        {
            Assert.NotNull(Validator, "LevelLayoutValidator not found.");
            MethodInfo m = Validator.GetMethods(BindingFlags.Public | BindingFlags.Static).SingleOrDefault(x => x.Name == method && x.GetParameters().Length == args.Length);
            Assert.NotNull(m, "LevelLayoutValidator." + method + " not found.");
            return m.Invoke(null, args);
        }

        static string[] Errors(string method, params object[] args) => ((IList)V(method, args)).Cast<string>().ToArray();

        static object Room(int sections = 2, bool precision = false, int jumps = 0) => Fixture("Room", sections, precision, jumps);
        static object Routes(int chain = 6, int side = 2, int dies = 0, int recovers = 2, int repeatJ = 0, int bait = 0, bool untagged = false) =>
            Fixture("Routes", chain, side, dies, recovers, repeatJ, bait, untagged);
        static string[] Content(int level, object room, object routes) => Errors("ValidateBand2Content", "FIX", level, room, routes);

        static void AssertMentions(string[] errors, string mention)
        {
            Assert.IsNotEmpty(errors, "expected an error mentioning '" + mention + "'.");
            Assert.IsTrue(errors.Any(e => e.Contains(mention)), "expected an error mentioning '" + mention + "':\n" + string.Join("\n", errors));
        }

        static void AssertNone(string[] errors) => Assert.IsEmpty(errors, string.Join("\n", errors));

        // ---------- counts ----------

        [Test] public void TheMinimums_EightLethal_SixInSequence_TwoDeadEnds_Pass() => AssertNone(Content(11, Room(), Routes()));

        [Test] public void SevenLethalBetrayals_IsRejected() => AssertMentions(Content(11, Room(), Routes(side: 1)), "lethal betrayals (distinct");

        [Test] public void FiveInSequence_IsRejected() => AssertMentions(Content(11, Room(), Routes(chain: 5, side: 3)), "in sequence");

        [Test] public void OneDeadEnd_IsRejected() => AssertMentions(Content(11, Room(), Routes(recovers: 1)), "dead end");

        [Test] public void Level10_IsNotBand2() => AssertNone(Content(10, Room(1), Routes(chain: 1, side: 0, recovers: 0)));

        // D-085's door distance, as band 1: at least half the room's width (15 u here) or height away from the start (x 2).
        [Test] public void TheDoorNearTheStart_IsRejected() => AssertMentions(Content(11, Fixture("RoomWithDoor", 16.9f), Routes()), "from the start");

        [Test] public void TheDoorHalfTheWidthAway_Passes() => AssertNone(Content(11, Fixture("RoomWithDoor", 17f), Routes()));

        // ---------- length and sections ----------

        // §13 R4: the floor is 1100 ticks (22 s), the ceiling 2500.
        [TestCase(11, 1050, false)] [TestCase(11, 1099, false)] [TestCase(11, 1100, true)] [TestCase(11, 2500, true)] [TestCase(11, 2501, false)] [TestCase(10, 900, true)]
        public void Duration_TwentyTwoToFiftySeconds(int level, int ticks, bool passes)
        {
            string[] errors = Errors("ValidateBand2Duration", "FIX", level, ticks);
            Assert.AreEqual(passes, errors.Length == 0, string.Join("\n", errors));
        }

        [TestCase(1, false)] [TestCase(2, true)] [TestCase(3, true)] [TestCase(4, false)]
        public void TwoOrThreeCheckpointSections(int sections, bool passes)
        {
            string[] errors = Content(11, Room(sections), Routes());
            Assert.AreEqual(passes, !errors.Any(e => e.Contains("checkpoint sections")), string.Join("\n", errors));
        }

        // ---------- answers (P7) ----------

        [Test] public void AnAnswerForFourTraps_IsRejected() => AssertMentions(Content(11, Room(), Routes(repeatJ: 4)), "answer J is the answer to 4 traps");

        [Test] public void AnAnswerForThreeTraps_Passes() => AssertNone(Content(11, Room(), Routes(repeatJ: 3)));

        [Test] public void TwoBaits_IsRejected() => AssertMentions(Content(11, Room(), Routes(bait: 2)), "answer BAIT");

        [Test] public void ABetrayalWithNoAnswerTag_IsRejected() => AssertMentions(Content(11, Room(), Routes(untagged: true)), "no answer tag");

        // ---------- precision placement ----------

        [Test] public void APrecisionSectionInLevel13_IsRejected() => AssertMentions(Content(13, Room(precision: true), Routes()), "precision section");

        [Test] public void APrecisionSectionInLevel15_Passes() => AssertNone(Content(15, Room(precision: true), Routes()));

        [Test] public void Level12_TwoJumpsInItsOneBeat_IsRejected() => AssertMentions(Content(12, Room(precision: true, jumps: 2), Routes()), "one beat");

        [Test] public void Level12_OneJumpInItsOneBeat_Passes() => AssertNone(Content(12, Room(precision: true, jumps: 1), Routes()));

        // ---------- the chaos moment ----------

        static string[] Chaos(int[] onsets, int extraTick, Func<int, int, bool> inView = null)
        {
            object replay = Fixture("ChaosReplay", onsets, extraTick);
            object result = V("Band2Chaos", replay, Fixture("ChaosRoom"), inView ?? ((i, e) => true));
            return Errors("ValidateBand2Chaos", "FIX", 11, result);
        }

        [Test] public void FiveOnsetsWithin60Ticks_Pass() => AssertNone(Chaos(new[] { 10, 20, 30, 40, 69 }, -1));

        [Test] public void FourOnsetsWithin60Ticks_IsRejected() => AssertMentions(Chaos(new[] { 10, 20, 30, 40, 70 }, -1), "chaos moment");

        [Test] public void TheDoorMarkerAndCatDontCount() => AssertMentions(Chaos(new[] { 10, 20, 30, 40 }, 25), "chaos moment");

        [Test] public void AnOnsetOffScreenDoesntCount() =>
            AssertMentions(Chaos(new[] { 10, 20, 30, 40, 50 }, -1, (i, e) => e != 2), "E2 t30 (off screen)");

        // ---------- the level's element in >= 3 betrayals ----------

        static string[] Element(int climbing, int revealed, int plain)
        {
            object routes = Fixture("VineRoutes", climbing, revealed, plain);
            object used = V("Band2ElementBetrayals", 14, Fixture("VineRoom"), routes, Fixture("VineReplays", routes), new Vector2(1f, .56f));
            return Errors("ValidateBand2Element", "FIX", 14, used);
        }

        [Test] public void TheVineInTwoBetrayals_IsRejected() => AssertMentions(Element(1, 1, 3), "used in 2");

        [Test] public void TheVineInThreeBetrayals_ByReplayAndByReveal_Passes() => AssertNone(Element(2, 1, 3));

        // The replay detectors for levels 11-13 (the vine's is above): the element counts only through the replay here.
        static string[] Acting(int level, int acting, int plain)
        {
            object routes = Fixture("ElementRoutes", acting, plain);
            object used = V("Band2ElementBetrayals", level, Fixture("ElementRoom", level), routes, Fixture("ElementReplays", level, routes), new Vector2(1f, .56f));
            return Errors("ValidateBand2Element", "FIX", level, used);
        }

        // Level 18 (half B) names three elements, the storm cloud, the geyser and the vine; its fixture room's is a geyser.
        // Level 19 names two, the spear and the inverter; its fixture room's is an inverter. Level 20 (the exam) names every
        // band-2 element; its fixture room's is a geyser. Level 17 names the spear and any chained trap; its fixture room's
        // is a spear.
        [TestCase(11)] [TestCase(12)] [TestCase(13)] [TestCase(17)] [TestCase(18)] [TestCase(19)] [TestCase(20)]
        public void TheElementActingInTwoBetrayals_IsRejected(int level) => AssertMentions(Acting(level, 2, 3), "used in 2");

        [TestCase(11)] [TestCase(12)] [TestCase(13)] [TestCase(17)] [TestCase(18)] [TestCase(19)] [TestCase(20)]
        public void TheElementActingInThreeBetrayals_Passes(int level) => AssertNone(Acting(level, 3, 3));
    }
}
