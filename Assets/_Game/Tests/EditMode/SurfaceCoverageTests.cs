using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Gameplay.Player;
using UnityEditor;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-080 (D-080) §5.3: LevelLayoutValidator.ValidateSurfaceCoverage. A betraying surface's danger is its top
    // strip; contact-triggered surfaces cover themselves, every other one needs a D-074 cut from its (root) trigger.
    public sealed class SurfaceCoverageTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static CatMotorConfig Motor() => AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
        static object Fixture(string name, params object[] args) => Call(T("RouteFixtures"), name, args);

        static List<string> Coverage(string levelId, object room) =>
            (List<string>)Validator.GetMethod("ValidateSurfaceCoverage").Invoke(null, new[] { levelId, room, (object)Motor() });

        static IEnumerable<(string id, object room)> RoomsInScope()
        {
            foreach (DictionaryEntry entry in (IDictionary)Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor").GetField("ById").GetValue(null))
                yield return ((string)entry.Key, entry.Value);
            var lab = (IList)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null);
            yield return ("TrapLab3", lab[3]);
            yield return ("TrapLab4", lab[4]);
        }

        [Test]
        public void AMovingAwaySolid_WithAGapInItsTrigger_Fails()   // seen red on the fixture
        {
            List<string> errors = Coverage("FIX", Fixture("MovingAwaySolidRoom", true));
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.IsTrue(errors.Any(e => e.Contains("Slide") && e.Contains("does not cut the cat's band")), string.Join("\n", errors));
        }

        [Test]
        public void AMovingAwaySolid_WithACutTrigger_Passes() =>
            CollectionAssert.IsEmpty(Coverage("FIX", Fixture("MovingAwaySolidRoom", false)));

        [Test]
        public void ContactTriggeredSurfaces_CoverThemselves() =>
            CollectionAssert.IsEmpty(Coverage("FIX", Fixture("ContactSurfacesRoom")));

        [Test]
        public void EveryRoomInScope_Passes()
        {
            var errors = new List<string>();
            foreach ((string id, object room) in RoomsInScope()) errors.AddRange(Coverage(id, room));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
        }

        // PAX-059: L004's FalseLanding, the one exemption, went with the redesign. The list is empty, and the frozen
        // pre-PAX-059 L004 (RouteFixtures.FastFlipRoom) still fails on FalseLanding, so the rule behind it holds.
        [Test]
        public void NoExemptions_AndThePrePax059L004FailsOnFalseLanding()   // seen red: FastFlipRoom
        {
            var exemptions = (IDictionary)Validator.GetField("SurfaceCoverageExemptions").GetValue(null);
            CollectionAssert.IsEmpty(exemptions.Keys.Cast<string>().ToArray());
            List<string> errors = Coverage("L004 before PAX-059", Fixture("FastFlipRoom"));
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            StringAssert.Contains("FalseLanding", errors[0]);
        }
    }
}
