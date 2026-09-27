using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-060 (D-093): the band-2 levels' route results as the route harness measured them when they were built: the
    // solution's length, every timed step's window, every betrayal's lead (by what reveals it) and every Recovers route.
    // ShippedRouteResultsTests keeps L001-L010.
    public sealed class Band2RouteResultsTests
    {
        IDisposable session;
        readonly Dictionary<string, object> reports = new();
        readonly Dictionary<string, int> ticks = new();

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            foreach (string id in new[] { "L011", "L012", "L013" })
            {
                object room = RouteValidatorTests.Room(id), routes = RouteValidatorTests.Routes(id);
                reports[id] = Call(T("RouteValidator"), "Run", session, id, room, routes);
                object solution = ReplayRoute(session, room, F(routes, "Solution"));
                List<Rec> records = Records(solution);
                ticks[id] = (bool)F(solution, "Completed") ? records[records.Count - 1].Tick : -1;
            }
        }

        [OneTimeTearDown] public void Close() => session?.Dispose();

        static string Summary(object report) => (string)report.GetType().GetMethod("Summary").Invoke(report, null);

        // §14 R2 (the padding pass) re-pinned all three: L011 gained Spear_L, Ledge_M and Spear_K; L012's inversions are no
        // longer waited out (1602 → 1323 ticks); L013 gained the Spikes_S count on S1. PAX-091: L011's Spikes_D2 trigger holds
        // Ledge_Hi's whole top strip, so D2's lead is 8 (was 7).
        [TestCase("L011", 1614, new[] { 32, 27, 27, 30, 44, 16 },
            new[] { "Spear_1=8", "Foot_C=21", "Spear_2=8", "Spear_L=8", "Ledge_M=93", "V1=8", "Spear_K=8", "Spear_Top=8", "Spear_Gap=12", "Lip=26", "Block_Door=7", "Spikes_D1=9", "Spikes_D2=8" }, new string[0])]
        [TestCase("L012", 1323, new[] { 31, 23, 37, 17, 51, 15, 33, 30, 31 },
            new[] { "Cat.Inverted=17", "Floor_1=14", "Floor_2=14", "Collapse_B=22", "Orb_A=55", "Spikes_D=28", "Block_E=7", "Collapse_F=19", "Cat.Inverted=912", "FakeFloor_D=25", "Cat.Inverted=1207", "Slot_Floor=8", "Spikes_D2=10" }, new string[0])]
        [TestCase("L013", 1613, new[] { 31, 28, 28, 28, 28, 36, 32, 34 },
            new[] { "Ledge_2=25", "Spikes_A=13", "Block_C=10", "Spikes_P2=1087", "G_D=1265", "Ledge_5=12", "Spikes_G=13", "Spikes_C1=1331", "Spikes_C2=1410", "Spikes_D2=11" },
            new[] { "Alcove visible t1172, complete t1913" })]
        public void RouteResults_AreAsBuilt(string id, int solutionTicks, int[] windows, string[] leads, string[] recoveries)
        {
            object report = reports[id];
            TestContext.Out.WriteLine(Summary(report));
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), Summary(report));
            Assert.AreEqual(solutionTicks, ticks[id], "solution ticks\n" + Summary(report));
            CollectionAssert.AreEquivalent(windows, ((IEnumerable)F(report, "Windows")).Cast<object>().Select(w => (int)F(w, "Count")).ToArray(), Summary(report));
            CollectionAssert.AreEquivalent(leads, ((IEnumerable)F(report, "Leads")).Cast<object>().Select(l => F(l, "RevealedBy") + "=" + F(l, "Lead")).ToArray(), Summary(report));
            CollectionAssert.AreEqual(recoveries, ((IEnumerable)F(report, "Recoveries")).Cast<object>()
                .Select(r => $"{F(r, "RevealedBy")} visible t{F(r, "FirstVisibleTick")}, complete t{F(r, "CompletionTick")}").ToArray(), Summary(report));
        }
    }
}
