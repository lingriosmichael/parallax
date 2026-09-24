using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.PrecisionTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-076 (D-083) §2.7/§6: a bait gap is out of reach at fraction 1.0, from the best take-off (full speed,
    // trailing side at the edge, 5 coyote ticks), by at least one run tick (0.12 u). Edge to edge that's 5.52 u
    // flat; the harness side (the attempt dies) is TrapLabRoom5Tests.TheBaitAttempt_FromTheBestTakeoff_Dies.
    public sealed class BaitGapTests
    {
        // Seen red at 6.5 u: "expected an error for a crossable bait gap" (a 6.5 u gap is out of reach).
        [Test]
        public void ABaitGapWithinReach_IsAnErrorNamingTheGap()
        {
            List<string> errors = Rule("ValidateBaitGaps", "Fixture", Fixture("BaitRoom", 5f), Motor(), Gravity());
            Assert.AreEqual(1, errors.Count, "expected an error for a crossable bait gap\n" + string.Join("\n", errors));
            StringAssert.Contains("bait gap Gap can be crossed", errors[0]);
            StringAssert.Contains("5.52", errors[0]);
        }

        [Test]
        public void ABaitGapJustBeyondTheMargin_Passes_AndJustInsideIt_Fails()
        {
            CollectionAssert.IsEmpty(Rule("ValidateBaitGaps", "Fixture", Fixture("BaitRoom", 5.66f), Motor(), Gravity()));
            Assert.AreEqual(1, Rule("ValidateBaitGaps", "Fixture", Fixture("BaitRoom", 5.6f), Motor(), Gravity()).Count);
        }

        [Test]
        public void Room5sBaitGap_Passes_AndTheLayoutPassesValidate()
        {
            object room5 = ((IList)System.Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null))[5];
            object gap = ((IEnumerable)RouteTestApi.F(room5, "BaitGaps")).Cast<object>().Single();
            var args = new object[] { gap, Motor(), Gravity(), 0f, 0f, 0f };
            Invoke(Validator, "BaitGapReach", args);
            TestContext.Out.WriteLine($"Exit_Gap: gap {(float)args[4]:F2} u, best reach {(float)args[3]:F2} u, margin {(float)args[4] - (float)args[3]:F2} u (needs {(float)args[5]:F2})");
            CollectionAssert.IsEmpty(Rule("ValidateBaitGaps", "TrapLab5", room5, Motor(), Gravity()));
        }
    }
}
