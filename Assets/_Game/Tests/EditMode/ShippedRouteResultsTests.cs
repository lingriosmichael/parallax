using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-080 §5.5: the route validator's changes (betrayal outcomes, soft-lock reporting, recording after a death)
    // leave every L001-L004 result exactly as D-079 (5) measured it.
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

        [TestCase("L001", new[] { 26 }, new int[0], new[] { "Collapse_C=21", "Spikes_A=24", "Block_A=17" })]
        [TestCase("L002", new[] { 26, 51 }, new[] { 16 }, new[] { "ReceiverBlock=12", "Collapse_C=21", "Sweep=24" })]
        [TestCase("L003", new[] { 15 }, new int[0], new[] { "Collapse_C=21", "CeilingSpikes=19", "ExitSpikes=22" })]
        [TestCase("L004", new[] { 17 }, new int[0], new[] { "SourceSpikes=21", "CeilingHiddenSpikes=20" })]
        public void RouteResults_AreUnchangedFromD079(string id, int[] windows, int[] margins, string[] leads)
        {
            object report = reports[id];
            TestContext.Out.WriteLine(Summary(report));
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), Summary(report));
            CollectionAssert.AreEquivalent(windows, ((IEnumerable)F(report, "Windows")).Cast<object>().Select(w => (int)F(w, "Count")).ToArray(), Summary(report));
            CollectionAssert.AreEquivalent(margins, ((IEnumerable)F(report, "Margins")).Cast<object>().Select(m => (int)F(m, "Value")).ToArray(), Summary(report));
            CollectionAssert.AreEquivalent(leads, ((IEnumerable)F(report, "Leads")).Cast<object>().Select(l => F(l, "RevealedBy") + "=" + F(l, "Lead")).ToArray(), Summary(report));
            Assert.AreEqual(0, ((IList)F(report, "Recoveries")).Count, "L001-L004 declare no Recovers betrayal (D-080)");
        }
    }
}
