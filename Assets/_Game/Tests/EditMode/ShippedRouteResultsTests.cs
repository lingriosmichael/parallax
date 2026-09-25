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
    // one Recovers betrayal (Retreat, revealed by the Door it moves), pinned per level with its ticks. PAX-059 (D-085):
    // re-pinned to the band-1 levels L001-L005 (L001 last), measured by the route harness.
    public sealed class ShippedRouteResultsTests
    {
        IDisposable session;
        readonly Dictionary<string, object> reports = new();

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            foreach (string id in new[] { "L001", "L002", "L003", "L004", "L005" })
                reports[id] = Call(T("RouteValidator"), "Run", session, id, RouteValidatorTests.Room(id), RouteValidatorTests.Routes(id));
        }

        [OneTimeTearDown] public void Close() => session?.Dispose();

        static string Summary(object report) => (string)report.GetType().GetMethod("Summary").Invoke(report, null);

        [TestCase("L001", new[] { 26 }, new int[0], new[] { "Floor_2=26", "Block_1=22", "Block_2=22", "Spikes_1=10", "Floor_7=26", "Spikes_2=6" }, new[] { "Door visible t213, complete t281" })]
        [TestCase("L002", new[] { 26, 26, 22, 21, 25, 31 }, new int[0], new[] { "Block_1=10", "Block_2=10", "Block_3=10", "Spikes_4=11", "Floor_9=34", "Tread_2=45", "Spikes_6=20" }, new string[0])]
        [TestCase("L003", new[] { 31, 34, 17, 32, 26 }, new int[0], new[] { "Floor_1=26", "Floor_3=21", "Spikes_B=18", "Block_5=10", "Spikes_R=20" }, new[] { "S1_Mid visible t374, complete t1132" })]
        [TestCase("L004", new[] { 12 }, new int[0], new[] { "Spikes_1=11", "Spikes_A=28", "Spikes_3=15", "Roof_4=21", "Roof_5=27", "Spikes_L=26" }, new string[0])]
        [TestCase("L005", new[] { 24 }, new int[0], new[] { "Arrow_A=6", "Arrow_B=6", "Ledge_Lo2=21", "Arrow_C=6", "Arrow_D=6", "Floor_6=27", "Spikes_D=15" }, new string[0])]
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
                "Recovers betrayals (D-080; PAX-059: L001 the Door, L003 S1_Mid; L002, L004 and L005 declare none)\n" + Summary(report));
        }
    }
}
