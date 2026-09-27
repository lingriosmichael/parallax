using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using static Parallax.Tests.EditMode.RouteTestApi;
using static Parallax.Tests.EditMode.PrecisionTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-093 (D-095) §2.6: Trap Lab room 12, one of each moving floor, through the route harness. Ride (start): Mover (Carry,
    // Periodic) over P1, Slider (Slip) in P2. Sink (gate x 32.5): Drop (Slip, Rearm, returns) in the deep P3, Shrink (from its
    // left, chasing) over P4. Shove (gate x 53.7): Pusher shoves a cat on the floor back into P4 (open space); Ledge_P is above
    // its path. Its routes and section rules are validated once per fixture; the tests read the cached reports.
    public sealed class TrapLabRoom12Tests
    {
        IDisposable session;
        object report, sections;

        internal static object Room() => ((IList)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null))[12];
        internal static object Routes() => Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room12");
        static string Summary(object r) => (string)r.GetType().GetMethod("Summary").Invoke(r, null);
        static IList List(object o, string field) => (IList)F(o, field);

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            report = Call(T("RouteValidator"), "Run", session, "TrapLab12", Room(), Routes());
            sections = Call(T("RouteValidator"), "ValidateSections", session, "TrapLab12", Room(), Routes(), null);
            TestContext.Out.WriteLine(Summary(report));
            foreach (string list in new[] { "Timings", "Respawns", "Rewinds" })
                foreach (object o in List(sections, list)) TestContext.Out.WriteLine(o.ToString());
        }

        [OneTimeTearDown] public void Close() => session?.Dispose();

        [Test]
        public void Layout_PassesEveryLayoutRule()
        {
            object room = Room();
            PlatformSizeConfig sizes = AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>("Assets/_Game/Data/PlatformSizeConfig.asset");
            LevelListConfig levels = AssetDatabase.LoadAssetAtPath<LevelListConfig>("Assets/_Game/Data/LevelListConfig.asset");
            var errors = new List<string>();
            errors.AddRange(Rule("Validate", "TrapLab12", room));
            errors.AddRange(Rule("ValidateMovingFloors", "TrapLab12", room, levels));
            errors.AddRange(Rule("ValidateSections", "TrapLab12", room));
            var bypasses = new List<string>();
            errors.AddRange(Rule("ValidateTriggerCoverage", "TrapLab12", room, Motor(), Gravity(), bypasses));
            errors.AddRange(Rule("ValidateSurfaceCoverage", "TrapLab12", room, Motor()));
            errors.AddRange(Rule("ValidatePlatformSizes", "TrapLab12", room, sizes));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
            CollectionAssert.IsEmpty(bypasses, "no learned bypass");
        }

        [Test]
        public void Routes_TheSolutionCompletes_AndEveryBetrayalIsAsDeclared() =>
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), Summary(report));

        // The numbers as built (PAX-093): the solution (933 ticks: Ride 584, Sink 177, Shove 172), the windows of the jump off
        // Slider (51, open both ways), off Drop (15) and up onto Ledge_P (25), and every betrayal's lead by what reveals it.
        [Test]
        public void RouteResults_AreAsBuilt()
        {
            List<Rec> records = Records(ReplayRoute(session, Room(), F(Routes(), "Solution")));
            string summary = Summary(report) + "\n" + string.Join("\n", List(sections, "Timings").Cast<object>());
            Assert.AreEqual(933, records[records.Count - 1].Tick, "solution ticks\n" + summary);
            CollectionAssert.AreEquivalent(new[] { 51, 15, 25 }, ((IEnumerable)F(report, "Windows")).Cast<object>().Select(w => (int)F(w, "Count")).ToArray(), summary);
            CollectionAssert.AreEquivalent(new[] { "Mover=60", "Slider=30", "Drop=25", "Shrink=56", "Pusher=74" }, ((IEnumerable)F(report, "Leads")).Cast<object>().Select(l => F(l, "RevealedBy") + "=" + F(l, "Lead")).ToArray(), summary);
        }

        [Test]
        public void Sections_EveryRewindIsExact_AndEverySectionIsInBudget() =>
            CollectionAssert.IsEmpty((IEnumerable)F(sections, "Errors"), string.Join("\n", List(sections, "Timings").Cast<object>()) + "\n" + string.Join("\n", List(sections, "Rewinds").Cast<object>()));
    }
}
