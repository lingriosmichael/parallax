using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-105 (D-110 (7), amendment 2): wall jumps don't skip a level's trolls. The reach model's wall-jump arc (WallJumpReach)
    // against the motor's numbers, every level through LevelLayoutValidator.ValidateWallJumpShortcuts (kinds (a)-(e), only
    // what wall jumps off grip walls add to the reach; L001-L020 have none and pass unchanged), and ValidateGripWall (G3).
    public sealed class WallJumpShortcutTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly Type Reach = Type.GetType("Parallax.Editor.Setup.WallJumpReach, Parallax.Editor");

        static object Arc() => Activator.CreateInstance(Reach, PrecisionTestApi.Motor(), PrecisionTestApi.Gravity(), 40f);
        static float Prop(object o, string name) => (float)o.GetType().GetProperty(name).GetValue(o);

        // The arc as the motor integrates it: the jump's full height above the cling, a crossing that latches at the top of
        // the rise in a 2 u shaft (WallClingHarnessTests measures the same through the route harness), and the D-110 note's
        // numbers.
        [Test]
        public void TheWallJumpArc_IsTheJumpsHeight_AndClimbsANarrowShaftAJumpAHop()
        {
            object arc = Arc();
            float apex = Prop(arc, "Apex"), crossing = Prop(arc, "CrossingReach"), back = Prop(arc, "ReturnOntoOwnTop");
            float hop2 = (float)Reach.GetMethod("HeightPerHop").Invoke(arc, new object[] { 2f });
            float hop4 = (float)Reach.GetMethod("HeightPerHop").Invoke(arc, new object[] { 4f });
            TestContext.Out.WriteLine($"apex {apex:F3}; crossing reach {crossing:F3} (face to face); height per hop: 2 u shaft {hop2:F3}, 4 u shaft {hop4:F3}; a wall top up to {back:F3} above the cling is reached back over");
            Assert.AreEqual(1.7f, apex, .01f, "a ground jump's height at 50 Hz (RouteHarnessFidelityTests: 1.700)");
            Assert.AreEqual(apex, hop2, .02f, "a 2 u shaft: latched at the top of the rise");
            Assert.Greater(crossing, 4f);
            Assert.Less(hop4, apex);
        }

        // G3: a grip wall at least 1.0 u tall, inside the room's frame, overlapping no other solid, not over a hazard.
        [TestCase("ok", null)]
        [TestCase("short", "a grip face needs at least")]
        [TestCase("outside", "outside the room's frame")]
        [TestCase("overlaps", "overlaps Slab")]
        [TestCase("hazard", "is over the hazard Spikes")]
        [TestCase("ceiling", "past the ceiling")]
        public void ValidateGripWall_Case(string what, string error)
        {
            object room = Type.GetType("Parallax.Editor.Setup.WallClingFixtures, Parallax.Editor").GetMethod("GripWallCase").Invoke(null, new object[] { what });
            var errors = (List<string>)Validator.GetMethod("ValidateGripWall").Invoke(null, new[] { "FIX", room, (object)PrecisionTestApi.Motor() });
            if (error == null) CollectionAssert.IsEmpty(errors);
            else Assert.IsTrue(errors.Any(e => e.Contains(error)), string.Join("\n", errors));
        }

        static IDictionary Layouts() => (IDictionary)Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor").GetField("ById").GetValue(null);

        public static IEnumerable<TestCaseData> Levels() =>
            Layouts().Keys.Cast<string>().OrderBy(id => id).Select(id => new TestCaseData(id).SetName($"WallJumpShortcuts_{id}"));

        [TestCaseSource(nameof(Levels))]
        public void NoWallJump_SkipsATroll(string id)
        {
            object layout = Layouts()[id];
            MethodInfo m = Validator.GetMethod("ValidateWallJumpShortcuts");
            var findings = (List<string>)m.Invoke(null, new[] { id, layout });
            foreach (string f in findings) TestContext.Out.WriteLine(f);
            CollectionAssert.IsEmpty(findings);
        }
    }
}
