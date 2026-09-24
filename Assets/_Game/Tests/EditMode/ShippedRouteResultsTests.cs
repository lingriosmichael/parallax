using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-080 §5.5: the route validator's changes (betrayal outcomes, soft-lock reporting, recording after a death)
    // leave every L001-L004 result exactly as D-079 (5) measured it. PAX-082 (D-082): re-pinned once to the new cat
    // (jump apex 1.6), the 20% faster traps and the refitted L001-L004 layouts and routes. PAX-083 (§12 R9): L001 declares
    // one Recovers betrayal (Retreat, revealed by the Door it moves), pinned per level with its ticks.
    public sealed class ShippedRouteResultsTests
    {
        IDisposable session;
        readonly Dictionary<string, object> reports = new();

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            foreach (string id in new[] { "L001", "L002", "L003", "L004" })
                reports[id] = Call(T("RouteValidator"), "Run", session, id, RouteValidatorTests.Room(id), RouteValidatorTests.Routes(id));
        }

        [OneTimeTearDown] public void Close() => session?.Dispose();

        static string Summary(object report) => (string)report.GetType().GetMethod("Summary").Invoke(report, null);

        [TestCase("L001", new[] { 26 }, new int[0], new[] { "Collapse_C=30", "Spikes_A=24", "Block_A=14" }, new[] { "Door visible t252, complete t345" })]
        [TestCase("L002", new[] { 26, 51 }, new[] { 16 }, new[] { "ReceiverBlock=10", "Collapse_C=21", "Sweep=23" }, new string[0])]
        [TestCase("L003", new[] { 26 }, new int[0], new[] { "Collapse_C=21", "CeilingSpikes=19", "ExitSpikes=15" }, new string[0])]
        [TestCase("L004", new[] { 20 }, new int[0], new[] { "SourceSpikes=21", "CeilingHiddenSpikes=20" }, new string[0])]
        public void RouteResults_AreUnchangedFromD079(string id, int[] windows, int[] margins, string[] leads, string[] recoveries)
        {
            object report = reports[id];
            TestContext.Out.WriteLine(Summary(report));
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), Summary(report));
            CollectionAssert.AreEquivalent(windows, ((IEnumerable)F(report, "Windows")).Cast<object>().Select(w => (int)F(w, "Count")).ToArray(), Summary(report));
            CollectionAssert.AreEquivalent(margins, ((IEnumerable)F(report, "Margins")).Cast<object>().Select(m => (int)F(m, "Value")).ToArray(), Summary(report));
            CollectionAssert.AreEquivalent(leads, ((IEnumerable)F(report, "Leads")).Cast<object>().Select(l => F(l, "RevealedBy") + "=" + F(l, "Lead")).ToArray(), Summary(report));
            CollectionAssert.AreEqual(recoveries, ((IEnumerable)F(report, "Recoveries")).Cast<object>()
                .Select(r => $"{F(r, "RevealedBy")} visible t{F(r, "FirstVisibleTick")}, complete t{F(r, "CompletionTick")}").ToArray(),
                "Recovers betrayals (D-080: L001 Retreat recovers, revealed by the Door; L002-L004 declare none)\n" + Summary(report));
        }
    }
}
