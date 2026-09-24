using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static Parallax.Tests.EditMode.PrecisionTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-076 (D-083) §6: precision sections. The fixture jump is 3.2 u: over D-056's 0.75 of the 3.92 u reach
    // (2.94), within the provisional 0.85 (3.33). Validate paths run against explicit thresholds; the committed
    // asset is TrapLabRoom5Tests' last test.
    public sealed class PrecisionSectionTests
    {
        readonly List<Object> created = new();

        [TearDown] public void Cleanup() { foreach (Object o in created) Object.DestroyImmediate(o); created.Clear(); }

        ScriptableObject T(float reach = .85f, int slack = 8) { ScriptableObject t = Thresholds(reach, slack); created.Add(t); return t; }
        static bool ReachError(IEnumerable<string> errors) => errors.Any(e => e.Contains("jump beyond"));

        [Test]
        public void JumpAtThePrecisionFraction_PassesInsideASection()
        {
            object room = Fixture("JumpRoom", "both");
            CollectionAssert.IsEmpty(Rule("ValidatePrecision", "Fixture", room, Motor(), Gravity(), T()));
            List<string> all = Rule("ValidateWithThresholds", "Fixture", room, T());
            CollectionAssert.IsEmpty(all, string.Join("\n", all));
        }

        [Test]
        public void TheSameJump_FailsOutsideASection()
        {
            List<string> errors = Rule("ValidateWithThresholds", "Fixture", Fixture("JumpRoom", "none"), T());
            Assert.IsTrue(errors.Any(e => e.Contains("jump beyond 0.75")), string.Join("\n", errors));
        }

        // Seen red with IsPrecisionJump accepting one end inside: the straddling jump then passed at 0.85.
        [Test]
        public void AStraddlingJump_IsCheckedAtD056sFraction()
        {
            object room = Fixture("JumpRoom", "takeoff");
            List<string> errors = Rule("ValidateWithThresholds", "Fixture", room, T());
            Assert.IsTrue(errors.Any(e => e.Contains("jump beyond 0.75")), string.Join("\n", errors));
            CollectionAssert.IsEmpty(Rule("ValidatePrecision", "Fixture", room, Motor(), Gravity(), T()), "a straddling jump isn't a precision jump, and isn't an error of its own");
        }

        [Test]
        public void TheConfigsValuesAreRead_ASecondConfigGivesADifferentResult()
        {
            object room = Fixture("JumpRoom", "both");
            CollectionAssert.IsEmpty(Rule("ValidatePrecision", "Fixture", room, Motor(), Gravity(), T(.85f)));
            List<string> tighter = Rule("ValidatePrecision", "Fixture", room, Motor(), Gravity(), T(.8f));
            Assert.IsTrue(tighter.Any(e => e.Contains("jump beyond 0.8 ")), string.Join("\n", tighter));
        }

        [Test]
        public void AMissingConfig_WithASection_IsAnError()
        {
            List<string> errors = Rule("ValidatePrecision", "Fixture", Fixture("JumpRoom", "both"), Motor(), Gravity(), null);
            Assert.IsTrue(errors.Any(e => e.Contains("need a PrecisionThresholds asset")), string.Join("\n", errors));
            CollectionAssert.IsEmpty(Rule("ValidatePrecision", "Fixture", Fixture("JumpRoom", "none"), Motor(), Gravity(), null), "no section, no config needed");
        }

        [Test]
        public void AConfigOutsideItsRange_IsAnError()
        {
            Assert.IsTrue(Rule("ValidatePrecision", "Fixture", Fixture("JumpRoom", "both"), Motor(), Gravity(), T(1f)).Any(e => e.Contains("outside 0.75")));
            Assert.IsTrue(Rule("ValidatePrecision", "Fixture", Fixture("JumpRoom", "both"), Motor(), Gravity(), T(.85f, 13)).Any(e => e.Contains("outside 0.75")));
        }

        // R1: a periodic trap wholly inside a section is held to the section's slack, not D-056's 12.
        [Test]
        public void APeriodicTrapInsideASection_UsesTheSectionsSlack()
        {
            List<string> outside = Rule("ValidateWithThresholds", "Fixture", Fixture("PeriodicRoom", false), T());
            Assert.IsTrue(outside.Any(e => e.Contains("periodic slack is below 12")), string.Join("\n", outside));
            List<string> inside = Rule("ValidateWithThresholds", "Fixture", Fixture("PeriodicRoom", true), T());
            CollectionAssert.IsEmpty(inside, string.Join("\n", inside));
            List<string> tighter = Rule("ValidatePrecision", "Fixture", Fixture("PeriodicRoom", true), Motor(), Gravity(), T(.85f, 11));
            Assert.IsTrue(tighter.Any(e => e.Contains("periodic slack is below 11")), string.Join("\n", tighter));
        }
    }
}
